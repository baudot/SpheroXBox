#Sphero with Xbox Controller

Use an xbox 360 controller to drive Sphero. This is the most recent code. It includes a few flourishes, like a game of tag built in, where the Sphero's alternate color to signal who's chasing and who's being chased. (One point to the chaser for each time they bump. Force feedback in the XBox controllers triggered by bumps.) 

###Getting Started
In the software, you need to associate a comm port with a Sphero. The Sphero's should already be connected to the computer via bluetooth. You still have to dig the comm port number out of the properties of the bluetooth device the Sphero appears as when you connect it.

Other than that, it's pretty straightforward: 
 
- Left stick steers.
- Right stick re-calibrates which direction is forward. The right bumper has to be held down simultaneously, to prevent accidental recalibration. Press "forward" on the left stick first, then push the right stick in the direction the Sphero thinks forward is. It will recalibrate so that its forward matches your forward.
- The left and right triggers are slow mode and fast mode. Hold them down. You return to medium speed by default.
- The main buttons change the color of the Sphero.
- The left bumper issues a Sphero API "brake" command. This only gets good results at the lowest speeds.
- The D-pad is unused.

###To Do

It also has some unfinished corners: the readouts to show which Sphero is connected to which comm port. That's not actually wired up.

