using System;
using System.Collections.Generic;
using System.Linq;

namespace MasterDataTool.EditorTools.Validation
{
    /// <summary>
    /// 複数の検証器を順に実行します。
    /// </summary>
    public sealed class ValidationRunner
    {
        readonly IValidator[] validators;

        /// <summary>
        /// 検証器一覧でランナーを初期化します。
        /// </summary>
        /// <param name="validators">検証器の列です。</param>
        public ValidationRunner(IEnumerable<IValidator> validators)
        {
            if (validators == null)
            {
                throw new ArgumentNullException(nameof(validators));
            }

            var list = validators as IList<IValidator> ?? validators.ToList();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    throw new ArgumentNullException(nameof(validators), "validators に null の要素が含まれています。");
                }

                if (string.IsNullOrEmpty(list[i].Id))
                {
                    throw new ArgumentException("Id が null または空の検証器があります。", nameof(validators));
                }
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < list.Count; i++)
            {
                if (!ids.Add(list[i].Id))
                {
                    throw new ArgumentException("Id が重複する検証器があります: " + list[i].Id, nameof(validators));
                }
            }

            this.validators = list.ToArray();
        }

        /// <summary>
        /// 登録された検証器を順に実行します。
        /// </summary>
        /// <param name="context">検証文脈です。</param>
        /// <returns>連結された検証結果です。</returns>
        public ValidationResult Run(ValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var results = new List<ValidationResult>(validators.Length);
            for (var i = 0; i < validators.Length; i++)
            {
                results.Add(RunValidator(validators[i], context));
            }

            return ValidationResult.Combine(results);
        }

        static ValidationResult RunValidator(IValidator validator, ValidationContext context)
        {
            try
            {
                var result = validator.Validate(context);
                if (result == null)
                {
                    return CreateValidatorFailure(validator.Id, "検証器が null を返しました。");
                }

                return result;
            }
            catch (Exception ex)
            {
                var message = "検証器 '" + validator.Id + "' で例外が発生しました: " +
                              ex.GetType().Name + ": " + ex.Message;
                return CreateValidatorFailure(validator.Id, message);
            }
        }

        static ValidationResult CreateValidatorFailure(string validatorId, string message)
        {
            var issue = new ValidationIssue(
                ValidationSeverity.Error,
                ValidationIssueKind.ValidatorFailure,
                validatorId,
                message);
            return new ValidationResult(new[] { issue });
        }
    }
}
