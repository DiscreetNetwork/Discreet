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
            Key.FromHex("c994ef3c17998f9ed3790947ee0f138485f6f26afa7f35abc57a5150cfbba1db"),
            Key.FromHex("bc871325d7d90147da84879e19bc5311f01af59da10919c071cc6372adde145f"),
            Key.FromHex("8b59acbf61731e063b4c44e6192e5f7091f881a5fc32f7e0e25fc1ed40be1d97"),
            Key.FromHex("2531a37ed03085acda521899e201bdbe023907097788210a45229847fa25dc2b"),
            Key.FromHex("db05003556a2874482bb84dde724d4c2674b5e7d817d20c36410299ead106695"),
            Key.FromHex("ef441d6d3faf83c89a8cf5417b1cafe41a6efdfa449240ab77fabbbf3a89813e"),
            Key.FromHex("969843a3de18f9395f68d163cc4250ee191fca995f4b99cccb535b716e8c8996"),
            Key.FromHex("41212dc902a1704796a264650b4026c4a04b1e4683b92d171cdc2f1ecabc2b36"),
            Key.FromHex("95cbe85e6739828e963aa35983eeecaa4d8a7d8da4e4791682d4774bd2a9b7cd"),
            Key.FromHex("697ba8e5aa035914f7941db5de1b4e048ad77db6ad5de10493df2cff1b51f0b7"),
            Key.FromHex("1241be44ddec0d253716777c46c44b1d7a04f6e5954bd90f556e4fae1b1db682"),
            Key.FromHex("09d2a4bfa98035747a9e6e0ce90affbbd2af6473faec8c749910554871917536"),
            Key.FromHex("13ded8880ed38664cb98e0644e080e0ab894ac76679ad54a0363dbf0863e186b"),
            Key.FromHex("e3062bc48a845c05e40e98c675b83df6a1e552f058b8048a902f608136659c9f"),
            Key.FromHex("7812eb4d5b7b0fc8f5ad1bd45f78cfa8a1023feb3efc89cda514df97e065b036"),
            Key.FromHex("f7cfc2e27cd6a4617cb5565832027a39314bf11fa800bf7fa91d519c87ddc62a")
        };

        public AuthKeys(DaemonConfig conf)
        {
            Keys = conf.AuConfig.AuthorityKeys.Select(x => Key.FromHex(x)).ToList();
            SigningKeys = conf.AuConfig.SigningKeys.Select(x => Key.FromHex(x)).ToList();
        }
    }
}
