#!/bin/bash
# /etc/init.d/irrigationcontroller

### BEGIN INIT INFO
# Provides:          irrigationcontroller
# Required-Start:    $remote_fs $syslog
# Required-Stop:     $remote_fs $syslog
# Default-Start:     2 3 4 5
# Default-Stop:      0 1 6
# Short-Description: Example initscript
# Description:       This service is used to manage the IrrigationController
### END INIT INFO


case "$1" in 
    start)
        echo "Starting IrrigationController"
        mono /home/pi/DotNet/IrrigationController/IrrigationController.exe
        ;;
    stop)
        echo "Stopping IrrigationController"
        killall IrrigationController.exe
        ;;
    *)
        echo "Usage: /etc/init.d/irrigationcontroller start|stop"
        exit 1
        ;;
esac

exit 0
