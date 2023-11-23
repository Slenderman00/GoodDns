//include regex
using System.Text.RegularExpressions;

namespace GoodDns.DNS.Server
{
    public class Record {
        Logging<Record> logger = new Logging<Record>("./log.txt", logLevel: 5);
        public string name;
        public int ttl;
        public RClasses _class = RClasses.IN;
        public RTypes type;
        public int priority;
        public string data;

        public RTypes getTypeByName(string name) {
            switch(name) {
                case "A":
                    return RTypes.A;
                case "AAAA":
                    return RTypes.AAAA;
                case "CNAME":
                    return RTypes.CNAME;
                case "MX":
                    return RTypes.MX;
                case "NS":
                    return RTypes.NS;
                case "PTR":
                    return RTypes.PTR;
                case "SOA":
                    return RTypes.SOA;
                case "TXT":
                    return RTypes.TXT;
                default:
                    return RTypes.A;
            }
        }

        public void parseLine(string line) {
            //remove trailing and leading whitespace
            line = line.Trim();
            //remove whitespace leaving only one space between each word
            line = Regex.Replace(line, @"\s+", " ");
            logger.Debug("line: " + line);
            //split the line into parts
            string[] parts = line.Split(' ');
        
            if(parts.Length < 3 || parts.Length > 4) {
                logger.Error("Invalid record: " + line);
                return;
            }

            logger.Debug("parts: " + string.Join(", ", parts));

            if(parts.Length == 3) {
                ttl = int.Parse(parts[0]);
                type = getTypeByName(parts[1]);
                data = parts[2];
                return;
            }

            if(parts.Length == 4 && parts[1] != "MX") {
                name = parts[0];
                ttl = int.Parse(parts[1]);
                type = getTypeByName(parts[2]);
                data = parts[3];
                return;
            }

            if(parts.Length == 4 && parts[1] == "MX") {
                ttl = int.Parse(parts[0]);
                type = getTypeByName(parts[1]);
                priority = int.Parse(parts[2]);
                data = parts[3];
                return;
            }

        }
    }

    //This class represents a BIND zone file
    //This dns server will base it anwsers on what is in these files and what is in the cache
    //Items that are not in the cache will be requested from a list of other specified dns servers

    //example zone file
    /*
    $ORIGIN example.com.
    @                      3600 SOA   ns1.p30.dynect.net. (
                                zone-admin.dyndns.com.     ; address of responsible party
                                2016072701                 ; serial number
                                3600                       ; refresh period
                                600                        ; retry period
                                604800                     ; expire time
                                1800                     ) ; minimum ttl
                        86400 NS    ns1.p30.dynect.net.
                        86400 NS    ns2.p30.dynect.net.
                        86400 NS    ns3.p30.dynect.net.
                        86400 NS    ns4.p30.dynect.net.
                        3600 MX    10 mail.example.com.
                        3600 MX    20 vpn.example.com.
                        3600 MX    30 mail.example.com.
                            60 A     204.13.248.106
                        3600 TXT   "v=spf1 includespf.dynect.net ~all"
    mail                  14400 A     204.13.248.106
    vpn                      60 A     216.146.45.240
    webapp                   60 A     216.146.46.10
    webapp                   60 A     216.146.46.11
    www                   43200 CNAME example.com.
    */

    public class BIND {
        Logging<BIND> logger = new Logging<BIND>("./log.txt", logLevel: 5);

        public string? origin = null;
        public string? primaryNameserver = null;
        public string? hostmaster = null;
        public string? serial = null;
        public string? refresh = null;
        public string? retry = null;
        public string? expire = null;
        public int? TTL = null;
        public int? minimumTTL = null;
        public List<Record> records;
        
        bool parsingSOA = false;

        private void parseRecord(string line) {
            string[] parts = line.Split(' ');
            //if line starts with $ORIGIN
            if (parts[0] == "$ORIGIN") {
                origin = parts[1];
            } else {
                Record record = new Record();
                record.parseLine(line);
                records.Add(record);
            }
        }

        /*
    @                      3600 SOA   ns1.p30.dynect.net. (
                                zone-admin.dyndns.com.     ; address of responsible party
                                2016072701                 ; serial number
                                3600                       ; refresh period
                                600                        ; retry period
                                604800                     ; expire time
                                1800                     ) ; minimum ttl

        */

        private void parseSOA(string line) {
            //remove )
            line = line.Replace("(", "");
            //remove whitespace leaving only one space between each word
            line = Regex.Replace(line, @"\s+", " ");
            //remove the @
            line = line.Substring(1);
            //remove everything after ;
            line = line.Split(';')[0];
            //remove trailing and leading whitespace
            line = line.Trim();
            string[] parts = line.Split(' ');
            //logger.Debug("line: " + line);
            //logger.Debug("parts: " + string.Join(", ", parts));
            //check if ttl is set
            if(TTL == null) {
                TTL = Int32.Parse(parts[0]);
                primaryNameserver = parts[2];
                logger.Debug("TTL: " + TTL);
                logger.Debug("primaryNameserver: " + primaryNameserver);
                return;
            }
            //check if array is empty
            if(parts.Length == 0) return;
            //check if hostmaster is set
            if(hostmaster == null) {
                hostmaster = parts[0];
                //strip these parts from the parts array
                parts = parts.Skip(1).ToArray();
                logger.Debug("hostmaster: " + hostmaster);
            }
            if(parts.Length == 0) return;
            //check if serial
            if(serial == null) {
                serial = parts[0];
                //remove this data from the parts array
                parts = parts.Skip(1).ToArray();
                logger.Debug("serial: " + serial);
            }
            if(parts.Length == 0) return;
            //check if refresh is set
            if(refresh == null) {
                refresh = parts[0];
                //remove this data from the parts array
                parts = parts.Skip(1).ToArray();
                logger.Debug("refresh: " + refresh);
            }
            if(parts.Length == 0) return;
            //check if retry is set
            if(retry == null) {
                retry = parts[0];
                //remove this data from the parts array
                parts = parts.Skip(1).ToArray();
                logger.Debug("retry: " + retry);
            }
            if(parts.Length == 0) return;
            //check if expire is set
            if(expire == null) {
                expire = parts[0];
                //remove this data from the parts array
                parts = parts.Skip(1).ToArray();
                logger.Debug("expire: " + expire);
            }
            if(parts.Length == 0) return;
            //check if minimumTTL is set
            if(minimumTTL == null) {
                minimumTTL = Int32.Parse(parts[0]);
                //remove this data from the parts array
                parts = parts.Skip(1).ToArray();
                logger.Debug("minimumTTL: " + minimumTTL);
            }
            if(parts.Length == 0) return;
            if(line.Contains(')')) {
                parsingSOA = false;
            }
            
        }

        private void parseLine(string line) {
            //if line starts with @
            if(line.StartsWith("@")) {
                parsingSOA = true;
            }
            if(!parsingSOA) {
                parseRecord(line);
            } else {
                parseSOA(line);
            }
        }

        public BIND(string path) {
            records = new List<Record>();
            string[] lines = File.ReadAllLines(path);
            foreach(string line in lines) {
                parseLine(line);
            }
        }
    }
}