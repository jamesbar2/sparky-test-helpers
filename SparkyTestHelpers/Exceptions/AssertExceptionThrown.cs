using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SparkyTestHelpers.Exceptions
{
    /// <summary>
    /// This class is used to assert than an expected exception is thrown when a
    /// test action is executed.
    /// </summary>
    /// <remarks>
    /// Why would you want to use this class instead of something like the
    /// VisualStudio TestTools ExpectedExceptionAttribute?
    /// <para>
    /// This class allows you to check the exception message.
    /// </para>
    /// <para>
    /// This class allows you to assert than exception is thrown for a specific
    /// statement, not just anywhere in the test method.
    /// </para>
    /// <para>
    /// There is no public constructor for this class. It is constructed using the
    /// "fluent" static factory method <see cref="OfType{TException}" /> or
    /// <see cref="OfTypeOrSubclassOfType{TException}" />.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    ///     AssertExceptionThrown
    ///         .OfType<ArgumentOutOfRangeException>()
    ///         .WithMessage("Limit cannot be greater than 10.")
    ///         .WhenExecuting(() => { var foo = new Foo(limit: 11); }); 
    /// ]]></code>  
    /// </example>
    public class AssertExceptionThrown
    {
        private readonly bool _allowDerivedTypes;
        private readonly Type _expectedExceptionType;

        private Func<string, string> _checkMessage = null;

        private AssertExceptionThrown(Type exceptionType, bool allowDerivedTypes = false)
        {
            _expectedExceptionType = exceptionType;
            _allowDerivedTypes = allowDerivedTypes;
        }

        /// <summary>
        /// Create new <see cref="AssertExceptionThrown" /> instance for testing
        /// that a specific exception type is thrown 
        /// when the <see cref="WhenExecuting(Action)" /> method is called.
        /// </summary>
        /// <typeparam name="TException">The exception type.</typeparam>
        /// <returns>
        /// New <see cref="AssertExceptionThrown" /> instance, ready to be changed to
        /// a "WithMessage..." method or the <see cref="WhenExecuting(Action)" /> method.
        /// </returns>
        public static AssertExceptionThrown OfType<TException>() where TException : Exception
        {
            return new AssertExceptionThrown(typeof(TException), allowDerivedTypes: false);
        }

        /// <summary>
        /// Create new <see cref="AssertExceptionThrown" /> instance for testing
        /// that a specific exception type or one of its subtypes is thrown
        /// when the <see cref="WhenExecuting(Action)" /> method is called.
        /// </summary>
        /// <typeparam name="TException">The base exception type.</typeparam>
        /// <returns>
        /// New <see cref="AssertExceptionThrown" /> instance, ready to be changed to
        /// a "WithMessage..." method or the <see cref="WhenExecuting(Action)" /> method.
        /// </returns>
        public static AssertExceptionThrown OfTypeOrSubclassOfType<TException>() where TException : Exception
        {
            return new AssertExceptionThrown(typeof(TException), allowDerivedTypes: true);
        }

        /// <summary>
        /// Set up to test that the <see cref="WhenExecuting(Action)" /> method
        /// throws an exception where the message exactly matches the <paramref name="expected" /> message.
        /// </summary>
        /// <param name="expected">The expected exception message.</param>
        /// <returns>The <see cref="AssertExceptionNotThrown" /> instance, ready to be "chained"
        /// to the <see cref="WhenExecuting(Action)" /> method.
        /// </returns>
        public AssertExceptionThrown WithMessage(string expected)
        {
            return SetCheckMessageFunction(actual => 
                (actual.Equals(expected, StringComparison.CurrentCulture)) 
                    ? null 
                    : $"Expected message \"{expected}\"");
        }

        /// <summary>
        /// Set up to test that the <see cref="WhenExecuting(Action)" /> method
        /// throws an exception where the message starts with the <paramref name="expected" /> message.
        /// </summary>
        /// <param name="expected">The expected exception message.</param>
        /// <returns>The <see cref="AssertExceptionNotThrown" /> instance, ready to be "chained"
        /// to the <see cref="WhenExecuting(Action)" /> method.
        /// </returns>
        public AssertExceptionThrown WithMessageStartingWith(string expected)
        {
            return SetCheckMessageFunction(actual =>
                (actual.StartsWith(expected, StringComparison.CurrentCulture))
                    ? null
                    : $"Expected message starting with \"{expected}\"");
        }

        /// <summary>
        /// Set up to test that the <see cref="WhenExecuting(Action)" /> method
        /// throws an exception where the message contains the <paramref name="expected" /> message.
        /// </summary>
        /// <param name="expected">The expected exception message.</param>
        /// <returns>The <see cref="AssertExceptionNotThrown" /> instance, ready to be "chained"
        /// to the <see cref="WhenExecuting(Action)" /> method.
        /// </returns>
        public AssertExceptionThrown WithMessageContaining(string expected)
        {
            return SetCheckMessageFunction(actual =>
                (actual.Contains(expected))
                    ? null
                    : $"Expected message containing \"{expected}\"");
        }

        /// <summary>
        /// Set up to test that the <see cref="WhenExecuting(Action)" /> method
        /// throws an exception where the message matches the <paramref name="regExPattern" />.
        /// </summary>
        /// <param name="regExPattern">The expected exception message.</param>
        /// <returns>The <see cref="AssertExceptionNotThrown" /> instance, ready to be "chained"
        /// to the <see cref="WhenExecuting(Action)" /> method.
        /// </returns>
        public AssertExceptionThrown WithMessageMatching(string regExPattern)
        {
            return SetCheckMessageFunction(actual =>
                (Regex.IsMatch(actual, regExPattern))
                    ? null
                    : $"Expected message matching \"{regExPattern}\"");
        }

        /// <summary>
        /// Call the <see cref="Action" /> that should throw an exception and assert that the exception was thrown.
        /// </summary>
        /// <param name="action">The <see cref="Action" /> that should throw an exception.</param>
        /// <returns>The caught exception.</returns>
        public Exception WhenExecuting(Action action)
        {
            Exception caughtException = null;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                Type type = ex.GetType();

                if (type == _expectedExceptionType || (_allowDerivedTypes && IsSubclass(ex, _expectedExceptionType)))
                {
                    if (_checkMessage != null)
                    {
                        string errorMessage = _checkMessage(ex.Message ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            throw new ExpectedExceptionNotThrownException(
                                $"{errorMessage}. Actual: \"{ex.Message}\".\n(message from {type.FullName}.)");
                        }
                    }
                }
                else
                {
                    throw; // Caught an exception, but it wasn't the expected type.
                }

                caughtException = ex;
            }

            if (caughtException == null)
            {
                throw new ExpectedExceptionNotThrownException(
                  $"Expected {_expectedExceptionType.FullName} was not thrown.");
            }

            return caughtException;
        }

        private bool IsSubclass(Exception ex, Type baseType)
        {
            return ex.GetType().GetTypeInfo().IsSubclassOf(baseType);
        }

        private AssertExceptionThrown SetCheckMessageFunction(Func<string, string> function)
        {
            if (_checkMessage != null)
            {
                throw new InvalidOperationException("Only one \"WithMessage...\" call is allowed.");
            }

            _checkMessage = function;

            return this;
        }
    }
}