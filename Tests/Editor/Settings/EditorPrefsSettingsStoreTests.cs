using MasterDataTool.EditorTools.Settings;
using NUnit.Framework;
using UnityEditor;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="EditorPrefsSettingsStore"/> の往復テストです。
    /// 使うキーはテスト専用で、SetUp と TearDown の両方で消します。
    /// </summary>
    public sealed class EditorPrefsSettingsStoreTests
    {
        const string Prefix = "MDTTests.EditorPrefsStore.";
        const string StringKey = Prefix + "String";
        const string BoolKey = Prefix + "Bool";
        const string EmptyKey = Prefix + "Empty";
        const string UnicodeKey = Prefix + "Unicode";

        static readonly string[] Keys = { StringKey, BoolKey, EmptyKey, UnicodeKey };

        EditorPrefsSettingsStore store;

        [SetUp]
        public void SetUp()
        {
            DeleteTestKeys();
            store = new EditorPrefsSettingsStore();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTestKeys();
        }

        [Test]
        public void String_RoundTrip()
        {
            store.SetString(StringKey, "C:/data/items.json");

            Assert.IsTrue(store.TryGetString(StringKey, out var value));
            Assert.AreEqual("C:/data/items.json", value);
        }

        [Test]
        public void Bool_RoundTrip()
        {
            store.SetBool(BoolKey, false);

            Assert.IsTrue(store.TryGetBool(BoolKey, out var value));
            Assert.IsFalse(value);

            store.SetBool(BoolKey, true);

            Assert.IsTrue(store.TryGetBool(BoolKey, out value));
            Assert.IsTrue(value);
        }

        [Test]
        public void TryGet_MissingKey_ReturnsFalse()
        {
            Assert.IsFalse(store.TryGetString(StringKey, out var text));
            Assert.IsNull(text);
            Assert.IsFalse(store.TryGetBool(BoolKey, out var flag));
            Assert.IsFalse(flag);
        }

        [Test]
        public void TryGetString_EmptyValue_ReturnsTrue()
        {
            store.SetString(EmptyKey, string.Empty);

            Assert.IsTrue(EditorPrefs.HasKey(EmptyKey));
            Assert.IsTrue(store.TryGetString(EmptyKey, out var value));
            Assert.AreEqual(string.Empty, value);
        }

        [Test]
        public void String_RoundTrip_JapaneseAndBackslash()
        {
            const string text = @"C:\データ\アイテム.json";
            store.SetString(UnicodeKey, text);

            Assert.IsTrue(store.TryGetString(UnicodeKey, out var value));
            Assert.AreEqual(text, value);
        }

        [Test]
        public void Cleanup_RemovesKeysAfterWrite()
        {
            store.SetString(StringKey, "残さない");
            store.SetBool(BoolKey, true);
            store.SetString(EmptyKey, string.Empty);
            store.SetString(UnicodeKey, @"C:\データ\アイテム.json");
            Assert.IsTrue(EditorPrefs.HasKey(StringKey));

            DeleteTestKeys();

            foreach (var key in Keys)
            {
                Assert.IsFalse(EditorPrefs.HasKey(key));
            }
        }

        static void DeleteTestKeys()
        {
            for (var i = 0; i < Keys.Length; i++)
            {
                EditorPrefs.DeleteKey(Keys[i]);
            }
        }
    }
}
