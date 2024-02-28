using Discreet.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discreet.Daemon.BlockAuth
{
    public class AuthKeys
    {
        public List<Key> Keys { get; set; }
        public List<Key> SigningKeys { get; set; }

        public static List<Key> Defaults = new List<Key>
        {
            Key.FromHex("478c9e6acd2550d9ddb5dc724adebba2e813529c09befa6b4305ce38079f129a"),
            Key.FromHex("14eaff489480db0153a828d51c4e92b0455db3d7852f23b4df242f846b773596"),
            Key.FromHex("d5511e4365d1f82cceed2c824dac98984dbd6cb1d39dce9f013db7cad35608fa"),
            Key.FromHex("7d402da6bc78912aacd311c16cb842f80d52f071d10c07291eaeddb9c60a53b7"),
            Key.FromHex("3a338b5ad2dd4eed00775ebafd9e595e161f1cdc199c92a905b7ca724599a4b9"),
            Key.FromHex("e52b19aac5ecc4b415004f737daaad66db2ff6b95dc6b61836b44dfa85dc1784"),
            Key.FromHex("054174ecb73c31d94a55bcb590f373ca957773e984d804fc08a647e788c5cc75"),
            Key.FromHex("76aa7818631f083c1dbf0de55ca93bad0901cc273b9528edd17374973042669c"),
            Key.FromHex("d76ddff86c8d9419c12c115b4c9dbad4947dc51c069fd52436c507a98dc2fb4a"),
            Key.FromHex("8f39f5756b58d6fd6f8a8a8f8317fe304012c16cc99a171ff4dfa65e00b85eb2"),
            Key.FromHex("743d533ad3982520f18ab9eca75e859443f7b5e42ec8d95e4a615982830294c9"),
            Key.FromHex("068bbe03cc0e618a0fbc866f89d50414b36de496f4439f61abae9e35528ed9c7"),
            Key.FromHex("da5c58806a747e281325964b8c639fb1010193ac3a3bac90b8182244d3e375a4"),
            Key.FromHex("f80cc33c39afff256986f28fe1bc151bc9ac03e6ad2866547cc641051e45c191"),
            Key.FromHex("a08e5ca857c4f7fb2f0240415f87a3c7e6160380481ece4c730e02768be4dafe"),
            Key.FromHex("556d49846cd3cbfd1f1a854a80514bc5e029d613146e3de329af994da6e4adb1")
        };

        public AuthKeys(DaemonConfig conf)
        {
            Keys = conf.AuConfig.AuthorityKeys.Select(x => Key.FromHex(x)).ToList();
            SigningKeys = conf.AuConfig.SigningKeys.Select(x => Key.FromHex(x)).ToList();
        }
    }
}
