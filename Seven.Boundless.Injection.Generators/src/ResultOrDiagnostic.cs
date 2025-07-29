using Microsoft.CodeAnalysis;

namespace Seven.Boundless.Injection.Generators {


	internal readonly struct ResultOrDiagnostic<T> {
		public bool HasResult => Result != null;
		public bool HasDiagnostic => Diagnostic != null;

		public T Result { get; }
		public Diagnostic Diagnostic { get; }


		public ResultOrDiagnostic(T result) {
			Result = result;
			Diagnostic = default;
		}

		public ResultOrDiagnostic(Diagnostic diagnostic) {
			Result = default;
			Diagnostic = diagnostic;
		}


		public static ResultOrDiagnostic<T> FromResult(T result) => new ResultOrDiagnostic<T>(result);
		public static ResultOrDiagnostic<T> FromDiagnostic(Diagnostic diagnostic) => new ResultOrDiagnostic<T>(diagnostic);

		public void Match(System.Action<T> resultAction, System.Action<Diagnostic> diagnosticAction, System.Action defaultAction = null) {
			if (HasResult) {
				resultAction(Result);
			}
			else if (HasDiagnostic) {
				diagnosticAction(Diagnostic);
			}
			else {
				defaultAction?.Invoke();
			}
		}


		public static implicit operator ResultOrDiagnostic<T>(T value) => FromResult(value);

		public static implicit operator ResultOrDiagnostic<T>(Diagnostic value) => FromDiagnostic(value);
	}
}