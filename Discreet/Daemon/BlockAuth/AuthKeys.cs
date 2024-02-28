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
            Key.FromHex("278e97176dcded4f2ae1737d94cdf89a912d35605ba75bb9405229277be6a571"),
            Key.FromHex("93d27ebf14c0b49bcc3dcc7a2043c709af899f3fdb8e5b30f4476eb4460f2c9f"),
            Key.FromHex("6622d9a0f291ea3b082a1d6f371ec33bd22e943f56e5f4cbada3d6c0c5de7a3c"),
            Key.FromHex("06f9dd3d77ca9273fec225da076b0c167e2fbc8b2ce3bb8a714c83c7217ee632"),
            Key.FromHex("f88785b89df395b182819b0c4ad4690d37491410902458f262d166c58af8fe0e"),
            Key.FromHex("b1edd92e26ea5211c14df50cee75d28c747285a1b761d6d9ceaedbdf6570b9cd"),
            Key.FromHex("6b4273b92ffbde6d8bec5a9f232b9051aadd36acd43bbbc8a950e0d725d19f49"),
            Key.FromHex("ab815fd16a44964e9b50367cc44dcf5f860e49050d3afeacf685168120d18f72"),
            Key.FromHex("16fef0a95c0d26c84591c5d182d7fd684b8631a38a439e5ce128427804b3baa1"),
            Key.FromHex("f4e02d87a0342a4b7ad6cbe3d06c033d6081c00301e28e89a76eeb5075454d78"),
            Key.FromHex("b8a0e039a64fc9757d54c0034224596895d437a7500566e87e90eadd983de827"),
            Key.FromHex("d829da97b9a8304801dfd1ad625f4e6a294372eb9cb1125d37745fc7780c2a93"),
            Key.FromHex("a3c624de57d508c7828184e9a96ad0ee483dd85b40b9aa7553b65419cacb21f9"),
            Key.FromHex("8e3903e795c4731d6e7fdada6968dc2f3514394c05c6b40b3b3fde2afdd27de2"),
            Key.FromHex("b5b635a3aa5bc52253520ea30725f7f5e939004600c577c3f7b66f4ba885ab03"),
            Key.FromHex("dd49a46a00d59025b2c247aad1898adf38bdcf8109b9b979bb7e50fdcd795da0")
        };

        public AuthKeys(DaemonConfig conf)
        {
            Keys = conf.AuConfig.AuthorityKeys.Select(x => Key.FromHex(x)).ToList();
            SigningKeys = conf.AuConfig.SigningKeys.Select(x => Key.FromHex(x)).ToList();
        }
    }
}
