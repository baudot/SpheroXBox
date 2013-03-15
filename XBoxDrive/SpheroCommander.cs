using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Sphero.Communicator;
using System.Diagnostics;

namespace SpheroFlick
{
    class SpheroCommander
    {
        internal SpheroCommunicator spheroComm;
        public bool spheroConnectionState = false;
        public bool spheroStabilizationState = true;
        public short spheroSpeed = (byte)180; //60 = Cautious, 120 = Comfortable, 255 = Crazy 
        public short spheroHeading = 0x0000; // 0 in 16bit Hex to start
        public byte spheroBackLED = 0x00;
        public TraceSource spheroTrace = new TraceSource("spheroTrace");

        public byte baseRed = 128;
        public byte baseGreen = 128;
        public byte baseBlue = 128;

        public ushort lastHeading;
        public bool wasAtRest = false;
        public ushort headingCorrection = 0;
        public ushort lastRightStickHeading = 0;
        public bool correctingHeading = false;
        public bool destabilized = false;

        public SpheroCommander()
        {
            spheroComm = new SpheroCommunicator();
        }

        public void Initialize(string portName)
        {
            bool done = false;
            byte tries = 0;
            while (!done)
            {
                try { 
                    spheroComm.openPort(portName);
                    done = true;
                }
                catch (Exception e) {
                    Debug.WriteLine("Error while connecting to Sphero: "+e);
                    tries++;
                    if (tries < 4) { done = false; }
                }
            }
            BackLED(0x0); // Off
            SetRGB(255, 0, 0);
            SetRotationRate(255);
            Stabilize(true);
            SetCollisionDetection();
            spheroConnectionState = true;
        }

        public void Roll(short magnitude, short heading)
        {
            spheroComm.write(PacketFactory.new_RollPacket((byte)magnitude, (byte)heading, true));
            spheroHeading = heading;
            spheroSpeed = magnitude;

            spheroTrace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "New Velocity: {0} {1}", spheroSpeed, spheroHeading);
        }

        public void ChangeDirection(short speed, short heading)
        {
            spheroSpeed += speed;
            spheroHeading += heading;
            spheroHeading %= 360;
            if (spheroSpeed > 255) spheroSpeed = 255;

            spheroComm.write(PacketFactory.new_RollPacket((byte)spheroSpeed, (ushort)spheroHeading, true));
            spheroTrace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "New Velocity: {0} {1}", spheroSpeed, spheroHeading);
        }

        public void BackLED(byte led)
        {
            spheroComm.write(PacketFactory.new_SetBackLEDPacket(led));
        }

        public void SetRGB(byte red, byte blue, byte green)
        {
            spheroComm.write(PacketFactory.new_SetRGBLEDPacket(red, green, blue, true));
        }

        public void Stabilize(bool enabled)
        {
            spheroComm.write(PacketFactory.new_SetStabilizationPacket(enabled));
        }

        public void SpinLeft()
        {
            spheroComm.write(PacketFactory.new_SetRawMotorValuesPacket(0x01, 0x80, 0x02, 0x80));
        }

        public void SpinRight()
        {
            spheroComm.write(PacketFactory.new_SetRawMotorValuesPacket(0x02, 0x80, 0x01, 0x80));
        }

        public void SetCollisionDetection()
        {
            spheroComm.write(PacketFactory.new_ConfigureCollisionDetectionPacket(0x01, 0x50, 0x28, 0x40, 0x28, 0x25));
        }

        public void GetConfigurationBlock()
        {
            spheroComm.write(PacketFactory.new_GetConfigBlockPacket(0x01));
        }

        public void Stop()
        {
            Roll(0, spheroHeading);
        }

        public void SetCalibration(short heading)
        {
            spheroComm.write(PacketFactory.new_SetCalibrationPacket((ushort)heading));
            spheroComm.write(PacketFactory.new_RollPacket(0, 0, true));
        }

        public void SetRotationRate(byte rotationRate)
        {
            spheroComm.write(PacketFactory.new_SetRotationRatePacket((byte)rotationRate));
        }

        public void ShiftColor(byte r, byte g, byte b)
        {
            byte nr = (byte)((baseRed*2 + r)/3);
            byte ng = (byte)((baseGreen*2 + g)/3);
            byte nb = (byte)((baseBlue*2 + b)/3);

            try { spheroComm.write(PacketFactory.new_SetRGBLEDPacket(nr, ng, nb, true));
            Debug.WriteLine("rgb: "+nr+" "+ng+" "+nb);
            }
            catch (SpheroCommunicationException ex) { Debug.WriteLine(ex.Message); }
        }

        public void Brighten(double intensity) {
            byte nr = (byte)(255 * intensity);
            byte ng = (byte)(255 * intensity);
            byte nb = (byte)(255 * intensity);
            nr = (nr > baseRed) ? nr : baseRed;
            ng = (ng > baseGreen) ? ng : baseGreen;
            nb = (nb > baseBlue) ? nb : baseBlue;

            try
            {
                spheroComm.write(PacketFactory.new_SetRGBLEDPacket(nr, ng, nb, true));
                Debug.WriteLine("rgb: " + nr + " " + ng + " " + nb);
            }
            catch (SpheroCommunicationException ex) { Debug.WriteLine(ex.Message); }
        }

        public void Dim(double intensity)
        {
            byte nr = (byte)(255 * intensity);
            byte ng = (byte)(255 * intensity);
            byte nb = (byte)(255 * intensity);
            nr = (nr < baseRed) ? nr : baseRed;
            ng = (ng < baseGreen) ? ng : baseGreen;
            nb = (nb < baseBlue) ? nb : baseBlue;

            try
            {
                spheroComm.write(PacketFactory.new_SetRGBLEDPacket(nr, ng, nb, true));
                Debug.WriteLine("rgb: " + nr + " " + ng + " " + nb);
            }
            catch (SpheroCommunicationException ex) { Debug.WriteLine(ex.Message); }
        }


        public void RevertToBaseColor()
        {
            try { spheroComm.write(PacketFactory.new_SetRGBLEDPacket(baseRed, baseGreen, baseBlue, true)); }
            catch (SpheroCommunicationException ex) { Debug.WriteLine(ex.Message); }
        }

        public bool IsConnected()
        {
            return spheroConnectionState;
        }
    }
}
