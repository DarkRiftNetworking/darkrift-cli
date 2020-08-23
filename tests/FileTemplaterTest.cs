using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DarkRift.Cli
{
    [TestClass]
    public class FileTemplaterTest
    {
        [TestMethod]
        [DataRow("my-plugin", "MyPlugin")]
        [DataRow("my plugin", "MyPlugin")]
        [DataRow("plugin", "Plugin")]
        [DataRow("PLUGIN", "Plugin")]
        [DataRow("my.plugin", "MyPlugin")]
        [DataRow("my1plugin", "My1Plugin")]
        public void TestNormalize(string input, string expectedOutput)
        {
            // WHEN the string is normalized
            string result = FileTemplater.Normalize(input);

            // THEN the string is normalized correctly
            Assert.AreEqual(expectedOutput, result);
        }

        [TestMethod]
        [DataRow('a', false)]
        [DataRow('1', false)]
        [DataRow('_', false)]
        [DataRow('.', true)]
        [DataRow(' ', true)]
        public void TestIsSpecialChar(char input, bool expectedOutput)
        {
            // WHEN the character is tested
            bool result = FileTemplater.IsSpecialChar(input);

            // THEN the result as expected
            Assert.AreEqual(expectedOutput, result);
        }

        // TODO test TemplateFileAndPath
    }
}
