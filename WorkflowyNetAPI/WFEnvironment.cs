namespace WorkflowyNetAPI
{
	/// Selects which WorkFlowy API host the client talks to.
	public enum WFEnvironment
	{
		/// https://workflowy.com/api/v1/
		Production,

		/// https://beta.workflowy.com/api/v1/ - required for the mirror endpoints.
		Beta
	}
}
