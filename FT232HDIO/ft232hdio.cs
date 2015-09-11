using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using FTD2XX_NET;

namespace DIO
{
    public class FT232HDIO
    {
         FTDI _ftdi = new FTDI();

        /// <summary>
        /// Track the state of the output pins
        /// </summary>
        byte _cha_state = 0x00;
        byte _chb_state = 0x00;

        /// <summary>
        /// BUSA = D[0:7], BUSB = C[0:7]
        /// </summary>
        public enum DIO_BUS { BUSA, BUSB };
        /// <summary>
        /// The pin numbers
        /// </summary>
        public enum PIN { PIN0=0, PIN1=1, PIN2=2, PIN3=3, PIN4=4, PIN5=5, PIN6=6, PIN7=7 };

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dev_index"></param>
        /// <returns></returns>
        public FTDI.FT_STATUS Init(uint dev_index = 0)
        {
            // Reste and purge any data
            ResetPort();
            _ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);

            // Check we have a device
            uint count = 0;
            _ftdi.GetNumberOfDevices(ref count);
            Debug.Assert(count > dev_index, string.Format("No FTDI device at channel {0}.  FTDI device count was {1}", dev_index, count));
            _ftdi.OpenByIndex(dev_index);

            // Verify is the right type
            FTDI.FT_DEVICE_INFO_NODE[] devlist = new FTDI.FT_DEVICE_INFO_NODE[100];
            FTDI.FT_STATUS status = _ftdi.GetDeviceList(devlist);
            FTDI.FT_DEVICE_INFO_NODE devinfo = devlist[dev_index];
            FTD2XX_NET.FTDI.FT_DEVICE expected = FTD2XX_NET.FTDI.FT_DEVICE.FT_DEVICE_232H;
            Debug.Assert(devinfo.Type == expected, string.Format("Unexpected device type '{0}' at index {1}.  Expected '{2}'", 
                devinfo.Type, dev_index, expected.ToString()));

            // Enable MPSSE
            _ftdi.SetBitMode(0xFF, FTDI.FT_BIT_MODES.FT_BIT_MODE_MPSSE);
            //_ftdi.SetBitMode(0, 0);
            //_ftdi.SetBitMode(0, 2);

            // Set all outputs and data = all 0
            //(0x80, level_low, dir_low, 0x82, level_high, dir_high)
            byte[] data = new byte[] { 0x80, _cha_state, 0xFF, 0x82, _chb_state, 0xFF };
            uint n = 0;
            status = _ftdi.Write(data, data.Length, ref n);
            return status;
        }

        /// <summary>
        /// Gets the address for each data bus
        /// </summary>
        /// <param name="bus"></param>
        /// <returns></returns>
        byte _get_bus_address(DIO_BUS bus)
        {
            byte addr = 0x80;
            if (bus == DIO_BUS.BUSB)
                addr = 0x82;
            return addr;
        }

        /// <summary>
        /// Gets the state of a the pins for a particular bus
        /// </summary>
        /// <param name="bus"></param>
        /// <returns></returns>
        byte _get_bus_state(DIO_BUS bus)
        {
            byte state = _cha_state;
            if (bus == DIO_BUS.BUSB)
                state = _chb_state;
            return state;
        }

        void _set_bus_state(DIO_BUS bus, byte state)
        {
            if (bus == DIO_BUS.BUSA)
                _cha_state = state;
            else if (bus == DIO_BUS.BUSB)
                _chb_state = state;
        }

        /// <summary>
        /// Set the state of the pin
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="pin"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public FTDI.FT_STATUS SetPin(DIO_BUS bus, int pin, bool value)
        {
            Debug.Assert(pin < 8 && pin >= 0, "Pin number must be between 0 and 7");
            byte addr = _get_bus_address(bus);
            byte state_current = _get_bus_state(bus);

            byte pin_num = Convert.ToByte(pin);
            byte state_new = state_current;
            if (value)
            {
                state_new |= (byte)(1 << pin_num);
            }
            else
            {
                state_new &= (byte)(0 << pin_num);
            }

            FTDI.FT_STATUS status = FTDI.FT_STATUS.FT_OK;
            if (state_current != state_new)
            {
                _set_bus_state(bus, state_new);

                byte[] buffer = new byte[] { addr, state_new, 0xFF };
                uint n = 0;
                status = _ftdi.Write(buffer, buffer.Length, ref n);
            }

            return status;
        }

        /// <summary>
        /// Set the state of the pin
        /// </summary>
        /// <param name="bus"></param>
        /// <param name="pin"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public FTDI.FT_STATUS SetPin(DIO_BUS bus, PIN pin, bool value)
        {
            byte pin_num = Convert.ToByte(pin);
            FTDI.FT_STATUS status = SetPin(bus, pin_num, value);
            return status;
        }

        /// <summary>
        /// Reset the port
        /// </summary>
        /// <returns></returns>
        public FTDI.FT_STATUS ResetPort()
        {
            FTDI.FT_STATUS status = _ftdi.ResetPort();
            return status;
        }

        /// <summary>
        /// Close the port
        /// </summary>
        /// <returns></returns>
        public FTDI.FT_STATUS Close()
        {
            FTDI.FT_STATUS status = _ftdi.Close();
            return status;
        }

    }
}
