#!/usr/bin/env node

const path = require('path');
const fs = require('fs');
const chokidar = require('chokidar');
const WebSocket = require('ws');



/**
 * Starts a websocket server on the local system, replicates targetted directories to all connected clients
 * Usage: ./ParallelReplicatorServer.js my_source_folder
 */



// poyfill Array.flat()
if (!Array.prototype.flat) {
  Object.defineProperty(Array.prototype, 'flat', {
    configurable: true,
    writable: true,
    value: function () {
      var depth =
        typeof arguments[0] === 'undefined' ? 1 : Number(arguments[0]) || 0;
      var result = [];
      var forEach = result.forEach;

      var flatDeep = function (arr, depth) {
        forEach.call(arr, function (val) {
          if (depth > 0 && Array.isArray(val)) {
            flatDeep(val, depth - 1);
          } else {
            result.push(val);
          }
        });
      };

      flatDeep(this, depth);
      return result;
    },
  });
}

// recursively find all files and directories in the given path
function recursive_files_and_dirs(dir_path) {
  return fs.readdirSync(dir_path).map(f => {
    if (fs.statSync(dir_path + "/" + f).isDirectory())
      return [ [ dir_path + "/" + f ], recursive_files_and_dirs(dir_path + "/" + f) ].flat();
    else
      return [ dir_path + "/" + f ];
  }).flat();
}

// process a new directory/file and upload it to all clients
function process_path_added(change_path) {
  var relative_change_path = path.relative(watch_path, change_path);

  if (relative_change_path === '')
    return;

  console.log('update: ', relative_change_path);
  if (fs.statSync(change_path).isDirectory()) {
    wss.clients.forEach(c => {
      if (c.readyState === WebSocket.OPEN)
        c.send("updatedirectory," + (new Buffer(relative_change_path)).toString('base64'));
    });
  } else {
    var data = fs.readFileSync(change_path);
    var data_base64 = data.toString('base64');

    wss.clients.forEach(c => {
      if (c.readyState === WebSocket.OPEN)
        c.send("update," + (new Buffer(relative_change_path)).toString('base64') + "," + data_base64);
    });
  }
}

// process a new directory/file and upload it to all clients
function process_path_removed(change_path) {
  var relative_change_path = path.relative(watch_path, change_path);

  console.log('delete: ', relative_change_path);
  wss.clients.forEach(c => {
    if (c.readyState === WebSocket.OPEN)
      c.send("delete," + (new Buffer(relative_change_path)).toString('base64'));
  });
}


if (process.argv.length < 3)
  throw "usage: node ./ParallelReplicatorServer.js <replicate-directory> [--sync]";

var watch_path = process.argv[2];
var allow_client_sync = process.argv[3] === '--sync';

// start our websocket server
var wss = new WebSocket.Server({ port: 8080 });
wss.on('connection', ws => {
  console.log("client connected!");

  ws.on('message', message => {
    // console.log(`Received message from client => ${message}: ${typeof message}`);

    if (allow_client_sync) {
      wss.clients.forEach(c => {
        if (c !== ws)
          if (c.readyState === WebSocket.OPEN)
            c.send(message);
      });

      watcher.unwatch(watch_path);

      var instructions = message.toString('utf8').split(',');
      var path = (new Buffer(instructions[1], 'base64')).toString('utf8');
      path = path.replace(/\.\./g, '');
      path = path.replace(/\\/g, '/');

      console.log('\t cmd: ' + instructions[0] + ' -> ' + path);

      try {
        if (instructions[0] === 'update') {
          var data = new Buffer(instructions[2], 'base64');
          fs.writeFileSync(watch_path + "/" + path, data);
        } else if (instructions[0] === 'updatedirectory') {
          fs.mkdirSync(watch_path + "/" + path);
        } else if (instructions[0] === 'delete') {
          if (fs.statSync(watch_path + "/" + path).isDirectory()) {
            fs.rmdirSync(watch_path + "/" + path, { force: true, recursive: true });
          } else {
            fs.unlinkSync(watch_path + "/" + path);
          }
        }
      } catch (e) {
        console.log("exception: ", e);
      }

      watcher.add(watch_path);
    }
  });
  // upload the current directory state to our target
  recursive_files_and_dirs(watch_path).forEach(p => process_path_added(p));
});
console.log("websocket server started on 8080!");


// watch the directory for changes
var watcher = chokidar.watch(watch_path);
watcher.on('all', (event, change_path) => {
  // console.log(event, change_path);
  if (event === 'unlink' || event === 'unlinkDir') {
    process_path_removed(change_path);
  } else {
    process_path_added(change_path);
  }
});
console.log("watching watch_path: ", watch_path);



