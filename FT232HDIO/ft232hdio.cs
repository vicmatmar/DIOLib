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

        byte _cha_state = 0x00;
        byte _chb_state = 0x00;

        public enum DIO_BUS { BUSA, BUSB }; // BUSA = D[0:7], BUSB = C[0:7]
        public enum PIN { PIN0=0, PIN1=1, PIN2=2, PIN3=3, PIN4=4, PIN5=5, PIN6=6, PIN7=7 };

        public void init(uint dev_index = 0)
        {
            uint count = 0;
            _ftdi.GetNumberOfDevices(ref count);
            Debug.Assert(count > dev_index, string.Format("No FTDI device at channel {0}.  FTDI device count was {1}", dev_index, count));
            _ftdi.OpenByIndex(dev_index);

            _ftdi.SetBitMode(0xFF, FTDI.FT_BIT_MODES.FT_BIT_MODE_MPSSE);

            //(0x80, level_low, dir_low, 0x82, level_high, dir_high)
            byte[] data = new byte[] { 0x80, _cha_state, 0xFF, 0x82, _chb_state, 0xFF };
            uint n = 0;
            _ftdi.Write(data, data.Length, ref n);

        }

        byte _get_bus_address(DIO_BUS bus)
        {
            byte addr = 0x80;
            if (bus == DIO_BUS.BUSB)
                addr = 0x82;
            return addr;
        }

        byte _get_bus_state(DIO_BUS bus)
        {
            byte state = _cha_state;
            if (bus == DIO_BUS.BUSB)
                state = _chb_state;
            return state;
        }

        public void setPin(DIO_BUS bus, byte pin_num, bool value)
        {
            Debug.Assert(pin_num < 8, "Pin number must be between 0 and 7");
            byte addr = _get_bus_address(bus);
            byte state = _get_bus_state(bus);

            if (value)
            {
                state |= (byte)(1 << pin_num);
            }
            else
            {
                state &= (byte)(1 << pin_num);
            }

            byte[] buffer = new byte[] { addr, state, 0xFF };
            uint n = 0;
            _ftdi.Write(buffer, buffer.Length, ref n);

        }

        public void setPin(DIO_BUS bus, PIN pin, bool value)
        {
            byte pin_num = Convert.ToByte(pin);
            setPin(bus, pin_num, value);
        }

    }
}
