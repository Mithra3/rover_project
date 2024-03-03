*******************************************************************************************
# Python Pathfinding
# This takes distance and direction as parameters from the JS server. Python was used only because I could get it to work with the bmm150 in the  
# limited time I had.  It would be better to have the C++ Lidar driver save distance and direction data in a specific memory block using pointers,  
# and then have a C program that would use that for pathfinding, as the feedback regarding obstacles would be immediate. 

import bmm150
import math
import time
import subprocess
import sys #access command line arguments
import RPi.GPIO as GPIO

# Pathfinding

GPIO.setmode(GPIO.BCM)
GPIO.setwarnings(False)

gpio6 = 6
gpio13 = 13
gpio19 = 19
gpio26 = 26

GPIO.setup(gpio6,GPIO.OUT)
GPIO.setup(gpio13,GPIO.OUT)
GPIO.setup(gpio19,GPIO.OUT)
GPIO.setup(gpio26,GPIO.OUT)

device = bmm150.BMM150()  # Bus number will default to 1

if __name__ == '__main__':

    x = sys.argv[1]
    y = sys.argv[2]
    direction = int(x)
    distance = int(y)


    print(f"Direction of destination: {direction}. Distance: {distance}mm")


    # takes distance and direction in degrees as parameter to check (0 = in front, 90 to the right, 180 behind, 270 left)
    def checkForObstacles(direction, distance):
        print(f"Checking for obstacle in {direction} direction, relative to Rover")

        # run c++ program which will output 360DegreeSnap.txt
        subprocess.call(['sh', '/home/rover/pathfinding/runSimpleGrabber.sh'])
        time.sleep(2)
        thetas = [0.0] * 1151
        distances = [0.0] * 1151
        # after waiting for the c++ to finish, open the file
        with open('lidar360Snap.txt', 'r') as file:
            lines = file.readlines()
        for i in range(len(lines)):
            values = lines[i].strip().split(' ')
            if len(values) >= 4 and ' ' not in values[0]:
#                print(f"i = {i}, values = {values} ")
                theta = float(values[1])
                distance = float(values[3])
                thetas[i] = theta
                distances[i] = distance
        # 360 degrees needs to be scaled to 1150 readings
        selectionFloat = direction*3.194
        # round and make into integer
        selection = int(round(selectionFloat))
        measurement = 0
        print(f"5 measurements to be taken starting from selection: {selection}")
        for i in range(5):
            measurement += distances[selection]
            selection += 1
            print(f"distance measured: {measurement}mm selection: {selection}")

        measurement = measurement/5
        if measurement < distance:
            # there is an obstacle
            print(f"Obstacle detected in {direction} direction: {measurement}mm")
            return True

        if measurement > distance:
            print(f"No obstacle found in {direction} direction within {measurement}mm")
            # no obstacle
            return False

    def driveForward():
        GPIO.output(gpio6,0)
        GPIO.output(gpio13,1)
        GPIO.output(gpio19,0)
        GPIO.output(gpio26,1)

    def moveDistance(amount):
        # if Rover travels 1000mm per second
        driveTime = amount/1000
        GPIO.output(gpio6,0)
        GPIO.output(gpio13,1)
        GPIO.output(gpio19,0)
        GPIO.output(gpio26,1)
        time.sleep(driveTime)
        stop()

    def reverse():
        GPIO.output(gpio6,1)
        GPIO.output(gpio13,0)
        GPIO.output(gpio19,1)
        GPIO.output(gpio26,0)

    def stop():
        GPIO.output(gpio6,0)
        GPIO.output(gpio13,0)
        GPIO.output(gpio19,0)
        GPIO.output(gpio26,0)

    def turnLeft():
        GPIO.output(gpio6,1)
        GPIO.output(gpio13,0)
        GPIO.output(gpio19,0)
        GPIO.output(gpio26,1)

    def turnRight():
        GPIO.output(gpio6,0)
        GPIO.output(gpio13,1)
        GPIO.output(gpio19,1)
        GPIO.output(gpio26,0)

    # 0 = north, 180/-180 = south, 90 = east, -90 = west
    def faceDirection(direction):
        currentX, currentY, z = device.read_mag_data()
        headingRads = math.atan2(currentX, currentY)
        headingDegs = math.degrees(headingRads)
        print(f"In faceDirection(), Currently facing: {headingDegs:.2f}. Working to face: {direction:.2f}")


        # get within 10 degrees
        while headingDegs > 10 or headingDegs < -10:

            x, y, z = device.read_mag_data()
            headingRads = math.atan2(x, y)
            headingDegs = math.degrees(headingRads)
            print(f"Currently facing: {headingDegs:.2f}. Working to face: {direction:.2f}")

            if headingDegs > direction:
                turnLeft()
                time.sleep(1)
                stop()
                time.sleep(1)
            if headingDegs < -direction:
                turnRight()
                time.sleep(1)
                stop()
                time.sleep(1)
            if headingDegs < direction + 10 and headingDegs > direction - 10:
                print(f"Finished!!! ")
                stop()
                return False

    # used if obstacle is detected
    def goAround(direction):
        print(f"Running goAround()")
        obstacle = False
        altDirection = direction + 90
        # check the right, assume 500mm is enough space
        distance = 500
        obstacle = checkForObstacles(altDirection, distance)
        if obstacle:
            print(f"Obstacle detected to the right, trying left ...")
            altDirection = altDirection + 90
            # check the left
            obstacle = checkForObstacles(altDirection, distance)
            if obstacle:
                # no path found, cancel pathfinding
                return False
            if not obstacle:
                print(f"Path to left is clear, facing left ...")
                faceDirection(altDirection)
                # assumes 500mm is enough to get arourd obstacle
                moveDistance(500)
                faceDirection(direction)
                moveDistance(distance)
                return True
        if not obstacle:
            print(f"Path to right is clear, facing right ...")
            faceDirection(altDirection)
            print(f"Moving 500 mm to get around obstacle")
            moveDistance(500)

    def getToDestination(direction, distance):
        # check if path is clear
        faceDirection(direction)
        obstacle = False
        obstacle = checkForObstacles(direction, distance)
        if obstacle:
            # get around the obstacle
            goAround(direction)
            # face direction of destination
            faceDirection(direction)
            # move the correct distance
            moveDistance(distance)
        if not obstacle:
            # move the correct distance
            moveDistance(distance)

# get initial data and program condition, which is run until finished
x, y, z = device.read_mag_data()
finished = False

#    checkForObstacles()

while not finished:

    headingRads = math.atan2(x, y)
    headingDegs = math.degrees(headingRads)

    print(f"X: {x:.2f}µT, Y: {y:.2f}µT, Z: {z:.2f}µT, Currently facing: {headingDegs:.2f}")


    finished = getToDestination(direction, distance)
  
