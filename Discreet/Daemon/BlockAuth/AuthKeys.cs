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
            Key.FromHex("dd38abf712037d9b20215cf3659dbcdd8291c5ab2c8175e8c95294cdca158f65"),
            Key.FromHex("26007b9ed0eee9f49512efcf3b412900f245643a7a387349fe8b0b9a99f9ba1e"),
            Key.FromHex("ee011d6217eafd061f10beaeb52d4b736e9cb608e7989ec221a0ab3d4cd386c2"),
            Key.FromHex("864d1d1b611b080d604b2d0d4736098f15d13955b8f8aece1253a83c805cd7cf"),
            Key.FromHex("c699c76cdc6c309fd2f0a3ca18a327e1e5ca6d82662ca2b6edd292b1b2ef7a3f"),
            Key.FromHex("09624336633b6be330341a3c76651680da9b606bc91df62b070faadb90eed5c2"),
            Key.FromHex("6089d5ca4a6437f42bbee575fb7502fdc4ed2a01a1e83aa1bdf29b2dcda9d8c6"),
            Key.FromHex("e752637cab10fc777a48a842122692027497daa79da7a70d86f480146a1fd576"),
            Key.FromHex("c88abcf82ba4daf9ae6484210046aab9160673983239eacf6e45a1ff64070623"),
            Key.FromHex("0e5bdfe1c84a3f8a0a21e43dfdd66107b4a3e102e1e44ff311421c4d4bceb1b4"),
            Key.FromHex("b61847ef5537d4cddac35f4ffc2dee5d4c8b4eac699a46742938a75ccace2a9f"),
            Key.FromHex("744939f653a7495af63cc7f43be4a8c7eeb7e546100486c1bcd8167c508ca2db"),
            Key.FromHex("12984797588e05eb8b3adc877e4fa21580b56b804e46d12d21313f28495a7d99"),
            Key.FromHex("25cd70e765e73f54fc2b1c9da3c0d9cfc375112805cf278ebf5923a880c6a024"),
            Key.FromHex("977b1dfa9d255384a69e6cef8e174e803bcc5742e3a00571386945cdb507fa73"),
            Key.FromHex("468e7297de8693e569850afc03dbd361767d3ba8d83bd3c2549edecbf08cbc6b")
        };

        public AuthKeys(DaemonConfig conf)
        {
            Keys = conf.AuConfig.AuthorityKeys.Select(x => Key.FromHex(x)).ToList();
            SigningKeys = conf.AuConfig.SigningKeys.Select(x => Key.FromHex(x)).ToList();
        }
    }
}
