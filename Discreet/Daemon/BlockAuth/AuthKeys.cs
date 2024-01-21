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
            Key.FromHex("554fdfa82121ac55400fdbfde3403f608843bfb7e2ae17f05eca6e2f70dda038"),
            Key.FromHex("d485ff6a2268e0bd2dc0c6b5d190044f8a84491b653b2b603d072ed6f13d1376"),
            Key.FromHex("ef72d2be9afe943b03f7eabe17fe53c327db6dc4012c4910daa196961b379eee"),
            Key.FromHex("6e4f9047ce3425ed2d11d495dd2cabf2ef6662e3bf77076c97a54a49480a3474"),
            Key.FromHex("8fcea78a412b004a76c32d2933f4a43b8f52592c9ca2508b63d0861a786568d9"),
            Key.FromHex("d51518eb9295241bd1452bcc5bedd526f480017feb5c62c05b4f895a447020a5"),
            Key.FromHex("b8c5314628de3ba604934ac7a7ed3347a3050de2fa94e3f531a9073f821d2f9d"),
            Key.FromHex("513fdd96d370d543613cdcee3560c042d0a99db1101d6b95686e1770abefabf8"),
            Key.FromHex("ee761e6a2d89329bd4ea86effd65b117b9721fd267924e475a084f14cdab985a"),
            Key.FromHex("21ce2b2ea5692e5a0654540ae2dcc698504d7c91c9c7510d899d7025e8191805"),
            Key.FromHex("a2dbfee8f2baf02ca05d7d9f145c281556e2da5bbf680463d02de502aef25533"),
            Key.FromHex("fae2f381da4ff1ab150e6f1cede5eec7dbd7ed97f049fca80fe0d8d2b02b5036"),
            Key.FromHex("e76bcc50c1cac70b6be1cce37139fd9badf884826eed1c8963426ca33175664a"),
            Key.FromHex("9195313d0a9240c221b3be02fc979ae1c754abf7471299283d58577d9f6c20f5"),
            Key.FromHex("11240f594192a1cbe2736dd6381d6a3abb51bd34c3edd34ba5c675c8c5238c65")
        };

        public AuthKeys(DaemonConfig conf)
        {
            Keys = conf.AuConfig.AuthorityKeys.Select(x => Key.FromHex(x)).ToList();
            SigningKeys = conf.AuConfig.SigningKeys.Select(x => Key.FromHex(x)).ToList();
        }
    }
}
