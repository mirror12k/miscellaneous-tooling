#!/bin/sh

if [ $# -ne 1 ]; then
	echo "this script packages a directory of your choice into a self-unpacking shell script"
	echo "usage: $0 <directory> > my_script.sh"
	echo "uses cat/sed/tar during unpacking, make sure those are available!"
	exit 1
fi

cat <<HEADER
#!/bin/sh
cat \$0 | sed -n '1,3!p' - | tar -zvxf -; exit 0
MAGIC
HEADER
tar -zcf - $1 | cat
