using System.Text;

namespace GoodDns.DNS
{
    public class Answer {
        Logging<Answer> logger = new Logging<Answer>("./log.txt", logLevel: 5);
        public string? domainName;
        public RTypes answerType;
        public RClasses answerClass;
        public uint ttl;
        public ushort dataLength;
        public byte[]? rData;

        public Answer(string domainName="", RTypes answerType=RTypes.A, RClasses answerClass=RClasses.IN, uint ttl=0, ushort dataLength=0, byte[] rData=null) {
            this.domainName = domainName;
            this.answerType = answerType;
            this.answerClass = answerClass;
            this.ttl = ttl;
            this.dataLength = dataLength;
            this.rData = rData;
        }

        public void Parse(ref byte[] answer, ref int currentPosition) {
            int pointer = (answer[currentPosition] << 8) | answer[currentPosition + 1];

            bool isPointer = (pointer & 0xC000) == 0xC000;

            //check if the current label is a compression pointer
            if (isPointer) {
                // Compression pointer found
                int offset = pointer & 0x3FFF; // Extract offset from the pointer
                //logger.Debug("Compression Pointer Found: " + offset);
                domainName = Utility.GetDomainName(answer, ref offset); // Get the domain name from the offset
            } else {
                // No compression pointer found
                domainName = Utility.GetDomainName(answer, ref currentPosition);
            }

            currentPosition += 2;

            answerType = (RTypes)((answer[currentPosition] << 8) | answer[currentPosition + 1]);
            currentPosition += 2;

            answerClass = (RClasses)((answer[currentPosition] << 8) | answer[currentPosition + 1]);
            currentPosition += 2;

            ttl = (uint)((answer[currentPosition] << 24) | (answer[currentPosition + 1] << 16) | (answer[currentPosition + 2] << 8) | answer[currentPosition + 3]);
            currentPosition += 4;

            dataLength = (ushort)((answer[currentPosition] << 8) | answer[currentPosition + 1]);
            currentPosition += 2;

            // Ensure that there is enough data in the array before trying to copy
            if (currentPosition + dataLength <= answer.Length) {
                rData = new byte[dataLength];
                for (int i = 0; i < dataLength; i++) {
                    rData[i] = answer[currentPosition++];
                }
            } else {
                // Handle the case where there is not enough data in the array
                logger.Debug("Error: Insufficient data in the array to read.");
            }
        }


        //re-implement in the same way as the question class
        public void Generate(ref byte[] packet, ref int currentPosition) {
            //add an answer to the packet
            //add the domain name
            string[] domainNameParts = this.domainName.Split('.');
            for (int j = 0; j < domainNameParts.Length; j++) {
                packet[currentPosition] = (byte)domainNameParts[j].Length;
                currentPosition++;
                for (int k = 0; k < domainNameParts[j].Length; k++) {
                    packet[currentPosition] = (byte)domainNameParts[j][k];
                    currentPosition++;
                }
            }

            //packet[currentPosition] = 0;
            currentPosition++;

            //add the answer type
            packet[currentPosition] = (byte)(((ushort)answerType) >> 8);
            packet[currentPosition+ 1] = (byte)(((ushort)answerType) & 0xFF);
            currentPosition += 2;

            //add the answer class
            packet[currentPosition] = (byte)(((ushort)answerClass) >> 8);
            packet[currentPosition + 1] = (byte)(((ushort)answerClass) & 0xFF);
            currentPosition += 2;

            //add the ttl
            packet[currentPosition] = (byte)(ttl >> 24);
            packet[currentPosition + 1] = (byte)(ttl >> 16);
            packet[currentPosition + 2] = (byte)(ttl >> 8);
            packet[currentPosition + 3] = (byte)(ttl & 0xFF);
            currentPosition += 4;


            //add the data length
            packet[currentPosition] = (byte)(dataLength >> 8);
            packet[currentPosition + 1] = (byte)(dataLength & 0xFF);
            currentPosition += 2;

            //add the rData
            for (int j = 0; j < rData.Length; j++) {
                packet[currentPosition] = rData[j];
                currentPosition++;
            }

            logger.Debug("Answer Generated");
            //log the bytes
            logger.Debug(BitConverter.ToString(packet).Replace("-", " "));
        }

        public void Print() {
            logger.Debug("Domain Name: " + domainName);
            logger.Debug("Answer Type: " + Enum.GetName(typeof(RTypes), answerType));
            logger.Debug("Answer Class: " + Enum.GetName(typeof(RClasses), answerClass));
            logger.Debug("TTL: " + ttl);
            logger.Debug("Data Length: " + dataLength);

            printData();
        }

        public void printData() {
            switch (answerType) {
                case RTypes.A:
                    logger.Debug("IP Address: " + rData[0] + "." + rData[1] + "." + rData[2] + "." + rData[3]);
                    break;
                case RTypes.NS:
                    logger.Debug("Name Server: " + Utility.GetDomainNameFromBytes(rData));
                    break;
                case RTypes.CNAME:
                    logger.Debug("Canonical Name: " + Utility.GetDomainNameFromBytes(rData));
                    break;
                case RTypes.SOA:
                    logger.Debug("Primary Name Server: " + Encoding.ASCII.GetString(rData));
                    break;
                case RTypes.MX:
                    logger.Debug("Mail Exchange: " + Utility.GetDomainNameFromBytes(rData));
                    break;
                case RTypes.TXT:
                    logger.Debug("Text: " + Encoding.ASCII.GetString(rData));
                    break;
                case RTypes.AAAA:
                    logger.Debug("IPv6 Address: " + rData[0] + "." + rData[1] + "." + rData[2] + "." + rData[3]);
                    break;
                case RTypes.SRV:
                    logger.Debug("Service: " + Utility.GetDomainNameFromBytes(rData));
                    break;
                default:
                    logger.Debug("Unknown Answer Type: " + answerType);
                    break;
            }
        }
    }
}