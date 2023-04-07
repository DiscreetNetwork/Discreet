using Discreet.Cipher;
using Discreet.Coin.Converters;
using Discreet.Coin.Models;
using Discreet.Common.Serialize;
using DiscreetUnit.Utilities;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit.Abstractions;

namespace DiscreetUnit.Coin
{
    public class CoinTests
    {
        private static List<TXInput> TXInputTestVectors = new List<TXInput>();
        private static List<string> TXInputJsonTestVectors = new(new string[]
        {
            "{\"Offsets\":[89443,271109,320774,661920,356562,198274,407358,731849,1046948,102265,700437,698430,785031,929010,620606,584343,102769,9549,111713,794434,774454,234702,464985,424038,281179,1028418,728040,766633,231703,788781,1041864,109538,1025158,1031679,553274,241440,137123,761861,172136,263000,240475,63611,75896,736703,452103,135818,1007464,327879,201816,678698,6971,690128,626225,853693,118419,903317,854699,945311,380741,376720,518809,689301,500145,258166],\"KeyImage\":\"5906dec4d175d41a409d3e76319f0c587efec9f17882cfe16e2346a4daee0c49\"}",
            "{\"Offsets\":[108976,590899,721848,801927,375736,379166,447220,389484,885988,897288,553958,710842,158805,776858,1020511,145360,20953,400161,564817,601581,781809,351736,531889,171465,913260,698533,435316,240387,381120,843992,792766,599750,612041,866305,627726,909780,462546,205537,1000719,871225,231477,666063,852715,777512,299734,456153,744511,77841,973376,235381,355766,118648,124484,947140,524932,805815,59009,550382,937242,725779,992425,206832,8364,41996],\"KeyImage\":\"51e24db6722b05eca3b04d78bf28df52bc6fd3f3018fe076afb839e401936864\"}",
        });

        private static List<TXInput> TXInputJsonTestTargets = new(new TXInput[]
        {
            new TXInput{Offsets = new uint[]{89443,271109,320774,661920,356562,198274,407358,731849,1046948,102265,700437,698430,785031,929010,620606,584343,102769,9549,111713,794434,774454,234702,464985,424038,281179,1028418,728040,766633,231703,788781,1041864,109538,1025158,1031679,553274,241440,137123,761861,172136,263000,240475,63611,75896,736703,452103,135818,1007464,327879,201816,678698,6971,690128,626225,853693,118419,903317,854699,945311,380741,376720,518809,689301,500145,258166 }, KeyImage = Key.FromHex("5906dec4d175d41a409d3e76319f0c587efec9f17882cfe16e2346a4daee0c49") },
            new TXInput{Offsets = new uint[]{108976,590899,721848,801927,375736,379166,447220,389484,885988,897288,553958,710842,158805,776858,1020511,145360,20953,400161,564817,601581,781809,351736,531889,171465,913260,698533,435316,240387,381120,843992,792766,599750,612041,866305,627726,909780,462546,205537,1000719,871225,231477,666063,852715,777512,299734,456153,744511,77841,973376,235381,355766,118648,124484,947140,524932,805815,59009,550382,937242,725779,992425,206832,8364,41996}, KeyImage = Key.FromHex("51e24db6722b05eca3b04d78bf28df52bc6fd3f3018fe076afb839e401936864") },
        });

        private static Random rng = new Random(3);
        private static JsonSerializerOptions options = new JsonSerializerOptions();
        private ITestOutputHelper output;

        public CoinTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        static CoinTests()
        {
            /* Initialize JsonSerializerOptions with the converters */
            new List<JsonConverter>(new JsonConverter[]
                {
                    // Coin.Converters
                    new BlockConverter(),
                    new BlockHeaderConverter(),
                    new BulletproofConverter(),
                    new BulletproofPlusConverter(),
                    new SignatureConverter(),
                    new TransactionConverter(),
                    new TTXInputConverter(),
                    new TTXOutputConverter(),
                    new TriptychConverter(),
                    new TXInputConverter(),
                    new TXOutputConverter(),

                }).ForEach(x => options.Converters.Add(x));

            /* initialize output */

            /* TXInput test vectors */
            for (int i = 0; i < 5; i++)
            {
                TXInput txInput = new TXInput();
                txInput.Offsets = Enumerable.Repeat(0U, 64).Select(x => (uint)rng.Next(1024 * 1024)).ToArray();
                txInput.KeyImage = KeyOps.GeneratePubkey();
                TXInputTestVectors.Add(txInput);
            }

            /* TXOutput */
        }

        public static bool TXInputEquals(TXInput x, TXInput y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;
            if (x.Offsets == y.Offsets && x.KeyImage == y.KeyImage) return true;
            if (y.Offsets == null || x.Offsets == null) return false;
            if (x.Offsets.Length != y.Offsets.Length) return false;
            if (x.Offsets.Length != 64) return false;

            for (int i = 0; i < x.Offsets.Length; i++)
            {
                if (x.Offsets[i] != y.Offsets[i]) return false;
            }

            return true;
        }

        [Fact]
        public void CheckTXInputConverterRandom()
        {
            foreach (var txinput in TXInputTestVectors)
            {
                string txinputstr = JsonSerializer.Serialize(txinput, txinput.GetType(), options);
                var txinput2 = JsonSerializer.Deserialize<TXInput>(txinputstr, options);
                Assert.True(EqualityUtil.CheckEqual(txinput, txinput2));
            }
        }

        [Fact]
        public void CheckTXInputConverterVectors()
        {
            var jsonToObjPairs = TXInputJsonTestVectors.Zip(TXInputJsonTestTargets);
            foreach ((string json, TXInput obj) in jsonToObjPairs)
            {
                string builtJson = JsonSerializer.Serialize(obj, obj.GetType(), options);
                Assert.Equal(builtJson, json);

                var builtObj = JsonSerializer.Deserialize<TXInput>(json, options);
                Assert.True(EqualityUtil.CheckEqual(obj, builtObj));
            }
        }

        [Fact]
        public void CheckTXInputSerializer()
        {
            foreach (var txinput in TXInputTestVectors)
            {
                byte[] serializedTxinput = txinput.Serialize();
                Assert.Equal(serializedTxinput.Length, txinput.Size);
                var reader = new MemoryReader(serializedTxinput);
                var deserializedTxinput = reader.ReadSerializable<TXInput>();
                Assert.True(EqualityUtil.CheckEqual(deserializedTxinput, txinput));
            }
        }
    }
}
