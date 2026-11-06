using System;
using System.Collections.Generic;
using MasterDataTool.EditorTools.Validation;
using MasterDataTool.Runtime;
using NUnit.Framework;

namespace MasterDataTool.EditorTools.Tests
{
    /// <summary>
    /// <see cref="ValidationRunner"/> のテストです。
    /// </summary>
    public class ValidationRunnerTests
    {
        static MasterTable SampleTable()
        {
            return new MasterTable(
                "TestItemMaster",
                typeof(TestItemMaster),
                typeof(TestItemRecord),
                MasterIdKind.Int,
                Array.Empty<IMasterRecord>(),
                null);
        }

        [Test]
        public void Run_NoValidators_ReturnsEmpty()
        {
            var runner = new ValidationRunner(Array.Empty<IValidator>());
            var result = runner.Run(new ValidationContext(new[] { SampleTable() }));
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Issues.Count);
        }

        [Test]
        public void Run_ConcatenatesInValidatorOrder()
        {
            var runner = new ValidationRunner(new IValidator[]
            {
                new FixedIssueValidator("a", ValidationIssueKind.ImportFailed, "A"),
                new FixedIssueValidator("b", ValidationIssueKind.TypeMismatch, "B"),
            });
            var result = runner.Run(new ValidationContext(new[] { SampleTable() }));

            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual("A", result.Issues[0].Message);
            Assert.AreEqual("B", result.Issues[1].Message);
        }

        [Test]
        public void Run_PassesSameContextInstance()
        {
            ValidationContext captured = null;
            var runner = new ValidationRunner(new IValidator[]
            {
                new CaptureContextValidator("capture", c => captured = c),
            });
            var context = new ValidationContext(new[] { SampleTable() });
            runner.Run(context);
            Assert.AreSame(context, captured);
        }

        [Test]
        public void Run_ValidatorThrows_AddsValidatorFailureAndContinues()
        {
            var runner = new ValidationRunner(new IValidator[]
            {
                new FixedIssueValidator("ok", ValidationIssueKind.ImportFailed, "ok"),
                new ThrowingValidator("boom"),
                new FixedIssueValidator("after", ValidationIssueKind.UnknownField, "after"),
            });
            var result = runner.Run(new ValidationContext(new[] { SampleTable() }));

            Assert.AreEqual(3, result.Issues.Count);
            Assert.AreEqual(ValidationIssueKind.ImportFailed, result.Issues[0].Kind);
            Assert.AreEqual(ValidationIssueKind.ValidatorFailure, result.Issues[1].Kind);
            StringAssert.Contains("boom", result.Issues[1].ValidatorId);
            StringAssert.Contains("InvalidOperationException", result.Issues[1].Message);
            Assert.AreEqual(ValidationIssueKind.UnknownField, result.Issues[2].Kind);
        }

        [Test]
        public void Run_ValidatorReturnsNull_AddsValidatorFailure()
        {
            var runner = new ValidationRunner(new IValidator[] { new NullResultValidator("nuller") });
            var result = runner.Run(new ValidationContext(new[] { SampleTable() }));

            Assert.AreEqual(1, result.Issues.Count);
            Assert.AreEqual(ValidationIssueKind.ValidatorFailure, result.Issues[0].Kind);
            Assert.AreEqual("nuller", result.Issues[0].ValidatorId);
        }

        [Test]
        public void Constructor_DuplicateId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ValidationRunner(new IValidator[]
            {
                new FixedIssueValidator("dup", ValidationIssueKind.ImportFailed, "1"),
                new FixedIssueValidator("dup", ValidationIssueKind.ImportFailed, "2"),
            }));
        }

        [Test]
        public void Constructor_EmptyId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new ValidationRunner(new IValidator[]
            {
                new FixedIssueValidator(string.Empty, ValidationIssueKind.ImportFailed, "x"),
            }));
        }

        [Test]
        public void Constructor_NullValidator_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ValidationRunner(new IValidator[] { null }));
        }

        [Test]
        public void Constructor_NullEnumerable_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new ValidationRunner(null));
        }

        [Test]
        public void Run_NullContext_ThrowsArgumentNullException()
        {
            var runner = new ValidationRunner(Array.Empty<IValidator>());
            Assert.Throws<ArgumentNullException>(() => runner.Run(null));
        }

        [Test]
        public void DefaultValidators_Create_ReturnsNewListEachTime()
        {
            var first = DefaultValidators.Create();
            var second = DefaultValidators.Create();
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.AreNotSame(first, second);
        }

        sealed class FixedIssueValidator : IValidator
        {
            readonly ValidationIssueKind kind;
            readonly string message;

            public FixedIssueValidator(string id, ValidationIssueKind kind, string message)
            {
                Id = id;
                this.kind = kind;
                this.message = message;
            }

            public string Id { get; }

            public ValidationResult Validate(ValidationContext context)
            {
                return new ValidationResult(new[]
                {
                    new ValidationIssue(ValidationSeverity.Error, kind, Id, message),
                });
            }
        }

        sealed class CaptureContextValidator : IValidator
        {
            readonly Action<ValidationContext> capture;

            public CaptureContextValidator(string id, Action<ValidationContext> capture)
            {
                Id = id;
                this.capture = capture;
            }

            public string Id { get; }

            public ValidationResult Validate(ValidationContext context)
            {
                capture(context);
                return ValidationResult.Empty;
            }
        }

        sealed class ThrowingValidator : IValidator
        {
            public ThrowingValidator(string id)
            {
                Id = id;
            }

            public string Id { get; }

            public ValidationResult Validate(ValidationContext context)
            {
                throw new InvalidOperationException("test failure");
            }
        }

        sealed class NullResultValidator : IValidator
        {
            public NullResultValidator(string id)
            {
                Id = id;
            }

            public string Id { get; }

            public ValidationResult Validate(ValidationContext context)
            {
                return null;
            }
        }
    }
}
