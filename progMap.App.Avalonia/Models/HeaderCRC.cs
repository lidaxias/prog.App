namespace progMap.App.Avalonia.Models
{
    /// <summary>
    /// Описывает алгоритм расчета CRC32
    /// </summary>
    public class HeaderCrc
    {
        #region const
        const uint CRC_POLYNOMIAL = 0x10211021;
        const int BITS_IN_BYTE_NMB = 8;
        const int CRC_BIT_NMB = 32;
        const int CRC_MASK = 1 << (CRC_BIT_NMB - 1);
        #endregion

        static uint[] lookupTable = new uint[256];
        static bool crc32_initialized = false;

        static void Crc32_Init()
        {
            ushort index1;
            ushort index2;

            for (index1 = 0; index1 < 256; index1++)
            {
                uint crc_table_value = (uint)(index1 << (CRC_BIT_NMB - BITS_IN_BYTE_NMB));

                for (index2 = 0; index2 < BITS_IN_BYTE_NMB; index2++)
                {
                    if (0 != (CRC_MASK & crc_table_value))
                    {
                        crc_table_value <<= 1;
                        crc_table_value ^= CRC_POLYNOMIAL;
                    }
                    else
                    {
                        crc_table_value <<= 1;
                    }
                }

                lookupTable[index1] = crc_table_value;
            }
            crc32_initialized = true;
        }

        public static uint Crc32(byte[] buffer, uint length)
        {
            uint crc_val = 0x0;
            uint index;

            if (!crc32_initialized)
                Crc32_Init();

            for (index = 0; index < length; index++)
            {
                crc_val ^= ((uint)buffer[index]) << (CRC_BIT_NMB - BITS_IN_BYTE_NMB);
                crc_val = (crc_val << BITS_IN_BYTE_NMB) ^ lookupTable[(byte)(crc_val >> (CRC_BIT_NMB - BITS_IN_BYTE_NMB))];
            }

            return crc_val;
        }
    }
}
