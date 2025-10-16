namespace SQLMultiScript.UI.ControlFactories
{
    public static class ControlExtensions
    {
        public static T Customize<T>(this T control, Action<T> configure)
            where T : Control
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            configure(control);

            return control;
        }
    }

}
