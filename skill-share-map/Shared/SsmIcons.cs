using SkillShareMap.Models;

namespace SkillShareMap.Shared
{
    /// <summary>
    /// Clean, Solar-style line icons (24×24, stroke = currentColor) shared by the
    /// home category cards, the <c>CategoryIcon</c> component and the Leaflet map
    /// markers (mirrored in <c>wwwroot/js/map.js</c>). Each constant is the *inner*
    /// SVG markup (no &lt;svg&gt; wrapper) so it drops straight into MudBlazor's
    /// MudIcon, which supplies a "0 0 24 24" viewBox.
    /// </summary>
    public static class SsmIcons
    {
        public const string StudyHelp =
            "<path d=\"M12 6.2C10.3 4.9 7.6 4.4 4.5 5v12.3c3.1-.6 5.8-.1 7.5 1.2 1.7-1.3 4.4-1.8 7.5-1.2V5c-3.1-.6-5.8-.1-7.5 1.2Z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linejoin=\"round\"/>" +
            "<path d=\"M12 6.2v12.3\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\"/>";

        public const string TechHelp =
            "<path d=\"M9.2 8.2 5 12l4.2 3.8M14.8 8.2 19 12l-4.2 3.8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>";

        public const string CreativeDesign =
            "<path d=\"M12 3.5C7.3 3.5 3.5 7 3.5 11.4c0 3.4 2.5 5.1 5.2 5.1 1.2 0 1.9-.8 1.9-1.8 0-.5-.2-.8-.2-1.2 0-.8.6-1.4 1.5-1.4H14c2.6 0 4.5-1.9 4.5-4.8C18.5 6.4 15.7 3.5 12 3.5Z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linejoin=\"round\"/>" +
            "<circle cx=\"7.6\" cy=\"11\" r=\"1.05\" fill=\"currentColor\"/>" +
            "<circle cx=\"10.4\" cy=\"7.6\" r=\"1.05\" fill=\"currentColor\"/>" +
            "<circle cx=\"14.4\" cy=\"7.8\" r=\"1.05\" fill=\"currentColor\"/>";

        public const string PhotoVideo =
            "<rect x=\"3\" y=\"7\" width=\"18\" height=\"13\" rx=\"3\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "<path d=\"M8.2 7 9.6 4.6h4.8L15.8 7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linejoin=\"round\"/>" +
            "<circle cx=\"12\" cy=\"13.4\" r=\"3.2\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>";

        public const string WritingEditing =
            "<path d=\"M5 19.2 6 15.6 15.6 6a1.9 1.9 0 0 1 2.7 2.7L8.7 18.3 5 19.2Z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linejoin=\"round\"/>" +
            "<path d=\"M14.2 7.4 16.9 10\" stroke=\"currentColor\" stroke-width=\"1.7\"/>";

        public const string LanguageHelp =
            "<path d=\"M5 4.5h10a2 2 0 0 1 2 2V12a2 2 0 0 1-2 2H9.5L6 16.8V14H5a2 2 0 0 1-2-2V6.5a2 2 0 0 1 2-2Z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linejoin=\"round\"/>" +
            "<path d=\"M6.5 8.2h7M6.5 10.7h4.5\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\"/>";

        public const string Job =
            "<rect x=\"3\" y=\"7.5\" width=\"18\" height=\"12\" rx=\"2.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "<path d=\"M8.5 7.5V6a2 2 0 0 1 2-2h3a2 2 0 0 1 2 2v1.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "<path d=\"M3 12.2h18\" stroke=\"currentColor\" stroke-width=\"1.7\"/>";

        public const string Help =
            "<circle cx=\"12\" cy=\"12\" r=\"8.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\"/>" +
            "<path d=\"M9.6 9.5a2.4 2.4 0 0 1 4.7.6c0 1.6-2.3 2-2.3 3.4M12 16.3h.01\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.7\" stroke-linecap=\"round\"/>";

        public static string ForCategory(TaskCategory c) => c switch
        {
            TaskCategory.StudyHelp => StudyHelp,
            TaskCategory.TechHelp => TechHelp,
            TaskCategory.CreativeDesign => CreativeDesign,
            TaskCategory.PhotoVideo => PhotoVideo,
            TaskCategory.WritingEditing => WritingEditing,
            TaskCategory.LanguageHelp => LanguageHelp,
            _ => Help
        };
    }
}
