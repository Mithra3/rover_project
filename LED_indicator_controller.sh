#!bin/bash

# This program starts with the weather data and server programs.
# It searches for them in running processes every 5 seconds
# if it finds them it drives their respective GPIOs high, turning on a
# green LED for each. If it doesn't find them it drives the GPIO low
# turning on the red LED, which indicates the state to the user.
# Some development debug messages can be seen commented out.

# set up H-Bridge GPIOs. This ensures the wheels don't start turning when the Pi boots
sudo echo 6 > /sys/class/gpio/export;
sudo echo 13 > /sys/class/gpio/export;
sudo echo 25 > /sys/class/gpio/export;
sudo echo 26 > /sys/class/gpio/export;
sudo echo out > /sys/class/gpio/gpio6/direction;
sudo echo out > /sys/class/gpio/gpio13/direction;
sudo echo out > /sys/class/gpio/gpio25/direction;
sudo echo out > /sys/class/gpio/gpio26/direction;
sudo echo 0 > /sys/class/gpio/gpio6/value;
sudo echo 0 > /sys/class/gpio/gpio13/value;
sudo echo 0 > /sys/class/gpio/gpio25/value;
sudo echo 0 > /sys/class/gpio/gpio26/value;

# set up LED indicators
sudo echo 21 > /sys/class/gpio/export;
sudo echo out > /sys/class/gpio/gpio21/direction;
sudo echo 20 > /sys/class/gpio/export;
sudo echo out > /sys/class/gpio/gpio20/direction;
sudo echo 16 > /sys/class/gpio/export;
sudo echo out > /sys/class/gpio/gpio16/direction;
sudo echo 12 > /sys/class/gpio/export;
sudo echo out > /sys/class/gpio/gpio12/direction;
sudo echo 5 > /sys/class/gpio/export;
sudo echo out > /sys/class/gpio/gpio5/direction;

while true; do

    # test to see if server is running, point output to null to block it from console
    if pgrep -f roverServer3 > /dev/null; then
        # gpio 21 for server
#        echo "roverServer is running"
        sudo echo 1 > /sys/class/gpio/gpio21/value;
    else
#        echo "roverServer not running"
        sudo echo 0 > /sys/class/gpio/gpio21/value;
    fi

    # test to see if weather data collection is running,
    if pgrep -f dataBME680 > /dev/null; then
        # gpio 20 for weather data
#        echo "dataBME680 is running"
        sudo echo 1 > /sys/class/gpio/gpio20/value;
    else
#        echo "dataBME680 is not running"
        sudo echo 0 > /sys/class/gpio/gpio20/value;
    fi

    # do this every 5 seconds
    sleep 5

done
