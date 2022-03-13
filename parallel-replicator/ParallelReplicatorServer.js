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

  console.log('update: ', relative_change_path);
  if (fs.statSync(change_path).isDirectory()) {
    wss.clients.forEach(c => {
      if (c.readyState === WebSocket.OPEN)
        c.send("updatedirectory," + (new Buffer(relative_change_path)).toString('base64') + ",");
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
      c.send("delete," + (new Buffer(relative_change_path)).toString('base64') + ",");
  });
}


if (process.argv.length < 3)
  throw "usage: node ./ParallelReplicatorServer.js <replicate-directory>";

var watch_path = process.argv[2];

// start our websocket server
var wss = new WebSocket.Server({ port: 8080 });
wss.on('connection', ws => {
  console.log("client connected!");

  ws.on('message', message => console.log(`Received message from client => ${message}`));
  // upload the current directory state to our target
  recursive_files_and_dirs(watch_path).forEach(p => process_path_added(p));
});
console.log("websocket server started on 8080!");


// watch the directory for changes
chokidar.watch(watch_path).on('all', (event, change_path) => {
  // console.log(event, change_path);
  if (event === 'unlink' || event === 'unlinkDir') {
    process_path_removed(change_path);
  } else {
    process_path_added(change_path);
  }
});
console.log("watching watch_path: ", watch_path);



