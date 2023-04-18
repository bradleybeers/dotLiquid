using DotLiquid;
using DotLiquid.FileSystems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TestDotLiquid;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void Test1()
        {
            var contentData = new Content
            {
                Devices = "Surface, Windows Phone, Monitors",
                User = new User() {FirstName = "Dean", LastName = "Ledet", Phone = "1678816156"}
            };
            var strData = JsonConvert.SerializeObject(contentData);

            var json = JsonConvert.DeserializeObject<IDictionary<string, object>>(strData, new DictionaryConverter());
            var jsonHash = Hash.FromDictionary(json);
            var templatetest = "{%- assign deviceList = Devices | split: ', ' -%}"
            + "<h1>{{User.FirstName | append: ' ' | append: User.LastName}}</h1><h2>{{User.FirstName | upcase}}</h2>"
            + "<h2><b>Phone</b>&nbsp; : {{User.Phone | slice: 1, 3}}</h2>"
            + "<h2><b>devices&nbsp; : <ul> {%- for d in deviceList -%}    "
            +                             "<li>{{d}}</li>"
            +                           " {%- endfor -%} </ul>";

            var template = Template.Parse(templatetest);
            var render = template.Render(jsonHash);
            System.Diagnostics.Debug.WriteLine(render);
        }

        [Test]
        public void HeaderFooterBodyTest()
        {
            string header = @"<html><body><H1>Riod</H1>";
            string footer = @"<br/><p>Private and confidential</p></body></html>";
            var template = ""      //Include the header template based on name Header
            + "{%- assign deviceList = Devices | split: ', ' -%}"
            + "<h1>{{User.FirstName | append: ' ' | append: User.LastName}}</h1><h2>{{User.FirstName | upcase}}</h2>"
            + "<h2><b>Phone</b>&nbsp; : {{User.Phone | slice: 1, 3}}</h2>"
            + "<h2><b>devices&nbsp; : <ul> {%- for d in deviceList -%}    "
            + "<li>{{d}}</li>"
            + " {%- endfor -%} </ul>"
            + "{% include Footer %} ";      //Include the footer template based on the name Footer

            var contentData = new Content
            {
                Devices = "Surface, Windows Phone, Monitors",
                User = new User() { FirstName = "Dean", LastName = "Ledet", Phone = "1678816156" }
            };
            var strData = JsonConvert.SerializeObject(contentData);

            Dictionary<string, string> templateMap = new Dictionary<string, string>();

            var templateStr = $"{{% include Header %}} {template} {{% include Footer %}}";
            templateMap.Add("Header", header);
            templateMap.Add("Footer", footer);
            templateMap.Add("ReportTemplate", templateStr);


            DotLiquidReportRenderer reportBuilder = new DotLiquidReportRenderer(DotLiquidTemplateLibrary.Create(templateMap));
            string renderedTemplate = reportBuilder.Render("ReportTemplate", strData);
            System.Diagnostics.Debug.WriteLine(renderedTemplate);

        }

        class User
        {
            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string Phone { get; set; }
        }

        class Content
        {
            public string Devices { get; set; }

            public User User { get; set; }

        }



        /// <summary>
        /// Represents a liquid template
        /// </summary>
        public class DotLiquidTemplate
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="DotLiquidTemplate" /> class.
            /// </summary>
            public DotLiquidTemplate(string name, string template)
            {
                Template = template;
                Name = name;
            }

            /// <summary>
            /// Gets the name.
            /// </summary>
            public string Name { get; private set; }

            /// <summary>
            /// Gets the template.
            /// </summary>
            public string Template { get; private set; }
        }

        /// <summary>
        /// Template library for DotLiquid this represents an in memory file system
        /// </summary>
        public class DotLiquidTemplateLibrary : IFileSystem
        {
            private readonly Dictionary<string, DotLiquidTemplate> _templateMap = new Dictionary<string, DotLiquidTemplate>();

            private DotLiquidTemplateLibrary(IEnumerable<DotLiquidTemplate> templates)
            {
                
                foreach (DotLiquidTemplate template in templates)
                {
                    _templateMap.Add(template.Name, template);
                }
            }

            /// <summary>
            /// Creates a DotLiquidTemplate library for the given dictionary which maps template names to Dot Liquid templates.
            /// </summary>
            public static DotLiquidTemplateLibrary Create(Dictionary<string, string> templates)
            {
                List<DotLiquidTemplate> dotLiquidTemplates =
                    templates.Keys.Select(templateName => new DotLiquidTemplate(templateName, templates[templateName])).ToList();
                DotLiquidTemplateLibrary templateLibrary = new DotLiquidTemplateLibrary(dotLiquidTemplates);
                return templateLibrary;
            }

            /// <summary>
            /// Reads the template file.
            /// </summary>
            public string ReadTemplateFile(Context context, string templateName)
            {
                return ReadTemplateFile(templateName);
            }

            /// <summary>
            /// Reads the template file.
            /// </summary>
            public string ReadTemplateFile(string templateName)
            {
                if (_templateMap.ContainsKey(templateName))
                {
                    return _templateMap[templateName].Template;
                }
                throw new KeyNotFoundException(string.Format("Unable to find dot liquid template called {0}", templateName));
            }
        }



        /// <summary>
        /// Responsible for rendering dot liquid templates
        /// </summary>
        public class DotLiquidReportRenderer
        {
            private readonly DotLiquidTemplateLibrary _templateLibrary;

            /// <summary>
            /// Initializes a new instance of the <see cref="DotLiquidReportBuilder"/> class.
            /// </summary>
            public DotLiquidReportRenderer(DotLiquidTemplateLibrary templateLibrary)
            {
                _templateLibrary = templateLibrary;
            }

            /// <summary>
            /// Renders the template combining it with the given jsonData
            /// </summary>
            public string Render(string reportTemplateName, string jsonData)
            {
                Template.FileSystem = _templateLibrary;

                var json = JsonConvert.DeserializeObject<IDictionary<string, object>>(jsonData, new DictionaryConverter());
                var jsonHash = Hash.FromDictionary(json);

                string template = _templateLibrary.ReadTemplateFile(reportTemplateName);
                Template compiledTemplate = Template.Parse(template);
                string text = compiledTemplate.Render(jsonHash);
                return text;
            }
        }
    }
}