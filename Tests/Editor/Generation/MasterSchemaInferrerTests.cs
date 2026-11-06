using System.Linq;
using MasterDataTool.EditorTools.Generation;
using MasterDataTool.EditorTools.Importing;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="MasterSchemaInferrer"/> のテストです。
    /// </summary>
    public class MasterSchemaInferrerTests
    {
        [Test]
        public void Infer_IntId_Succeeds()
        {
            const string json = "{\"records\":[{\"id\":1,\"value\":10}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(MasterIdKind.Int, result.Schema.IdKind);
        }

        [Test]
        public void Infer_StringId_Succeeds()
        {
            const string json = "{\"records\":[{\"id\":\"A\",\"value\":10}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(MasterIdKind.String, result.Schema.IdKind);
        }

        [Test]
        public void Infer_IntegerField_IsInt()
        {
            const string json = "{\"records\":[{\"id\":1,\"price\":100}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.Int, result.Schema.Fields[0].Type);
            Assert.IsFalse(result.Schema.Fields[0].IsList);
        }

        [Test]
        public void Infer_LargeIntegerField_IsLong()
        {
            const string json = "{\"records\":[{\"id\":1,\"bigCount\":5000000000}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.Long, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_FloatField_IsFloat()
        {
            const string json = "{\"records\":[{\"id\":1,\"ratio\":1.5}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.Float, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_IntegerAndFloatMixed_IsFloat()
        {
            const string json = "{\"records\":[{\"id\":1,\"ratio\":1},{\"id\":2,\"ratio\":1.5}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.Float, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_BoolField_IsBool()
        {
            const string json = "{\"records\":[{\"id\":1,\"enabled\":true}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.Bool, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_StringField_IsString()
        {
            const string json = "{\"records\":[{\"id\":1,\"name\":\"Potion\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.String, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_DateLikeString_IsString()
        {
            const string json = "{\"records\":[{\"id\":1,\"startedAt\":\"2026-01-01\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.String, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_PartialNull_UsesOtherRecords()
        {
            const string json = "{\"records\":[{\"id\":1,\"name\":null},{\"id\":2,\"name\":\"A\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.String, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_AllNull_IsString()
        {
            const string json = "{\"records\":[{\"id\":1,\"name\":null},{\"id\":2,\"name\":null}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.String, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_IntArray_IsListInt()
        {
            const string json = "{\"records\":[{\"id\":1,\"rewards\":[1,2]}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsTrue(result.Schema.Fields[0].IsList);
            Assert.AreEqual(SchemaFieldType.Int, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_StringArray_IsListString()
        {
            const string json = "{\"records\":[{\"id\":1,\"tags\":[\"a\"]}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsTrue(result.Schema.Fields[0].IsList);
            Assert.AreEqual(SchemaFieldType.String, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_ArrayIntAndFloatMixed_IsListFloat()
        {
            const string json = "{\"records\":[{\"id\":1,\"values\":[1,2.5]}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.Float, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_EmptyArrayOnly_IsListString()
        {
            const string json = "{\"records\":[{\"id\":1,\"tags\":[]},{\"id\":2,\"tags\":[]}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsTrue(result.Schema.Fields[0].IsList);
            Assert.AreEqual(SchemaFieldType.String, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_PartialEmptyArray_UsesOtherRecords()
        {
            const string json = "{\"records\":[{\"id\":1,\"tags\":[]},{\"id\":2,\"tags\":[\"a\"]}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(SchemaFieldType.String, result.Schema.Fields[0].Type);
        }

        [Test]
        public void Infer_FieldOrder_FollowsFirstAppearance()
        {
            const string json = "{\"records\":[{\"id\":1,\"b\":1,\"a\":2},{\"id\":2,\"a\":3,\"b\":4}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual("b", result.Schema.Fields[0].JsonName);
            Assert.AreEqual("a", result.Schema.Fields[1].JsonName);
        }

        [Test]
        public void Infer_KeyOnlyInSomeRecords_IsIncluded()
        {
            const string json = "{\"records\":[{\"id\":1,\"extra\":1},{\"id\":2}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.AreEqual(1, result.Schema.Fields.Count);
            Assert.AreEqual("extra", result.Schema.Fields[0].JsonName);
        }

        [Test]
        public void Infer_MissingId_Fails()
        {
            const string json = "{\"records\":[{\"id\":1},{\"value\":1}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsNull(result.Schema);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.MissingId));
        }

        [Test]
        public void Infer_InconsistentIdType_Fails()
        {
            const string json = "{\"records\":[{\"id\":1},{\"id\":\"A\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.InconsistentIdType));
        }

        [Test]
        public void Infer_IdNull_Fails()
        {
            const string json = "{\"records\":[{\"id\":null}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.InconsistentIdType));
        }

        [Test]
        public void Infer_IdOutOfRange_Fails()
        {
            const string json = "{\"records\":[{\"id\":5000000000}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.IdOutOfRange));
        }

        [Test]
        public void Infer_InconsistentFieldType_IntAndString_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"value\":1},{\"id\":2,\"value\":\"a\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.InconsistentFieldType));
        }

        [Test]
        public void Infer_InconsistentFieldType_ScalarAndArray_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"value\":1},{\"id\":2,\"value\":[1]}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.InconsistentFieldType));
        }

        [Test]
        public void Infer_InconsistentFieldType_ArrayElements_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"values\":[1,\"a\"]}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.InconsistentFieldType));
        }

        [Test]
        public void Infer_UnsupportedObjectField_IsWarningAndSkipped()
        {
            const string json = "{\"records\":[{\"id\":1,\"meta\":{\"a\":1},\"name\":\"A\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, result.Schema.Fields.Count);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.UnsupportedFieldValue));
        }

        [Test]
        public void Infer_UnsupportedObjectArray_IsWarningAndSkipped()
        {
            const string json = "{\"records\":[{\"id\":1,\"items\":[{\"a\":1}]}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(0, result.Schema.Fields.Count);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.UnsupportedFieldValue));
        }

        [Test]
        public void Infer_UnsupportedIntegerOutOfLongRange_IsWarningAndSkipped()
        {
            const string json = "{\"records\":[{\"id\":1,\"huge\":18446744073709551615}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(result.Schema.Fields.Any(f => f.JsonName == "huge"));
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.UnsupportedFieldValue));
        }

        [Test]
        public void Infer_InvalidFieldName_Uppercase_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"Name\":\"A\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            var issue = result.Issues.First(i => i.Kind == GenerationIssueKind.InvalidFieldName);
            StringAssert.Contains("英小文字で始めてください", issue.Message);
        }

        [Test]
        public void Infer_InvalidFieldName_ID_IncludesGuidance()
        {
            const string json = "{\"records\":[{\"id\":1,\"ID\":\"A\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            var issue = result.Issues.First(i => i.FieldName == "ID");
            StringAssert.Contains("\"id\"", issue.Message);
        }

        [Test]
        public void Infer_InvalidFieldName_Keyword_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"class\":1}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.InvalidFieldName));
        }

        [Test]
        public void Infer_InvalidFieldName_Symbol_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"item-name\":1}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
        }

        [Test]
        public void Infer_InvalidFieldName_LeadingDigit_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"1item\":1}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
        }

        [Test]
        public void Infer_ReservedFieldName_key_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"key\":\"A\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.ReservedFieldName));
        }

        [Test]
        public void Infer_ReservedFieldName_idKind_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"idKind\":1}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.ReservedFieldName));
        }

        [Test]
        public void Infer_ReservedFieldName_toString_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"toString\":\"A\"}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
        }

        [Test]
        public void Infer_ReservedFieldName_getHashCode_Fails()
        {
            const string json = "{\"records\":[{\"id\":1,\"getHashCode\":1}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
        }

        [Test]
        public void Infer_EmptyRecords_Fails()
        {
            const string json = "{\"records\":[]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.EmptyRecords));
        }

        [Test]
        public void Infer_InvalidJson_IsSourceError()
        {
            const string json = "{";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.SourceError));
        }

        [Test]
        public void Infer_RootArray_IsSourceError()
        {
            const string json = "[]";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            Assert.IsTrue(result.Issues.Any(i => i.Kind == GenerationIssueKind.SourceError));
        }

        [Test]
        public void Infer_DuplicateKey_IsSourceErrorWithLineNumber()
        {
            const string json = "{\"records\":[{\"id\":1,\"a\":1,\"a\":2}]}";
            var result = MasterSchemaInferrer.Infer(json);

            Assert.IsFalse(result.Succeeded);
            var issue = result.Issues.First(i => i.Kind == GenerationIssueKind.SourceError);
            Assert.Greater(issue.LineNumber, 0);
        }
    }
}
