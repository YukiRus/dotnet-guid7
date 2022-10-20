using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UUID7Gen
{
    public static class Guid7
    {
        private static int sequence = 0;
        private static object syncRoot = new object();//加锁对象
        private static long sequenceBits = 8L; // seq位长度
        private static int sequenceMask = (int)(-1L ^ -1L << (int)sequenceBits); // seq位最大值
        private static long lastTimestamp = -1L;//最后时间戳

        /// <summary>
        /// 生成当前时间戳
        /// </summary>
        /// <returns>纳秒</returns>
        public static long GetTimestamp()
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var nanoTimeStamp = Convert.ToInt64( ts.TotalNanoseconds);
            return nanoTimeStamp;
        }

        /// <summary>
        /// 获取下一微秒时间戳
        /// </summary>
        /// <param name="lastTimestamp"></param>
        /// <returns></returns>
        private static long GetNextTimestamp(long lastTimestamp)
        {
            var timestamp = GetTimestamp();
            if (timestamp <= lastTimestamp)
            {
                timestamp = GetTimestamp();
            }
            return timestamp;

        }
        private static Guid GenGuid()
        {
            lock (syncRoot)
            {
                long timestamp = GetTimestamp();
                if (Guid7.lastTimestamp == timestamp)
                { //同一微妙中生成ID
                    sequence = (sequence + 1) & sequenceMask; //用&运算计算该微秒内产生的计数是否已经到达上限
                    if (sequence == 0)
                    {
                        //一微妙内产生的ID计数已达上限，等待下一微妙
                        timestamp = GetNextTimestamp(Guid7.lastTimestamp);
                    }
                }
                else
                {
                    //不同微秒生成ID
                    sequence = 0;
                }
                if (timestamp < lastTimestamp)
                {
                    throw new Exception("时间戳比上一次生成ID时时间戳还小，故异常");
                }


                TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                //var ss = Convert.ToInt64(ts.TotalSeconds);
                var s = (int)(ts.TotalNanoseconds / 1000000000);
                var sec = s;
                var subSec = Convert.ToInt32(ts.TotalNanoseconds % 1000000000);

                byte[] uuidByte1;

                var sec28bit = sec >> 4;

                uuidByte1 = BitConverter.GetBytes(sec28bit);


                byte[] uuidByte2;

                var secEnd4Bit = sec & 0xF;
                var paragraph2Bit = (secEnd4Bit << 12) | (subSec >> 20);

                subSec &= 0xFFFFF;

                paragraph2Bit <<= 4;
                paragraph2Bit |= 7;

                paragraph2Bit <<= 12;
                paragraph2Bit |= (subSec >> 8);

                subSec &= 0xFF;

                uuidByte2 = BitConverter.GetBytes(paragraph2Bit);


                byte[] uuidByte3;
                var paragraph3Bit = 0b10;

                paragraph3Bit <<= 8;
                paragraph3Bit |= subSec;

                paragraph3Bit <<= 6;
                //var randomByte1 = RandomNumberGenerator.GetBytes(4);
                //var random1 = BitConverter.ToInt32(randomByte1) & 0x3f;
                //paragraph3Bit |= random1;

                paragraph3Bit <<= 8;
                paragraph3Bit |= sequence; // todo seq

                paragraph3Bit <<= 8;
                var random2 = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4)) & 0xff;
                paragraph3Bit |= random2;
                uuidByte3 = BitConverter.GetBytes(paragraph3Bit);
                Array.Reverse(uuidByte3);


                var uuidByte4 = RandomNumberGenerator.GetBytes(4);


                var uuidByte = new byte[16];
                uuidByte1.CopyTo(uuidByte, 0);
                uuidByte2.CopyTo(uuidByte, 4);
                uuidByte3.CopyTo(uuidByte, 8);
                uuidByte4.CopyTo(uuidByte, 12);


                var uuidByte34 = new byte[8];
                uuidByte3.CopyTo(uuidByte34, 0);
                uuidByte4.CopyTo(uuidByte34, 4);


                var guidb = BitConverter.ToInt16(new byte[] { uuidByte2[2], uuidByte2[3] });

                var guidc = BitConverter.ToInt16(new byte[] { uuidByte2[0], uuidByte2[1] });

                //var guid = new Guid(uuidByte);
                var guid = new Guid(
                    BitConverter.ToInt32(uuidByte1),
                    guidb,
                    guidc,
                    uuidByte34


                    );



                return guid;
            }

        }
        public static Guid NewGuid() { return GenGuid(); }
    }
   
}
