using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Skoruba.IdentityServer4.STS.Identity.Helpers.TagHelpers
{
    [HtmlTargetElement("img-gravatar")]
    public class GravatarTagHelper : TagHelper
    {
        [HtmlAttributeName("userId")]
        public string UserId { get; set; }

        [HtmlAttributeName("alt")]
        public string Alt { get; set; }

        [HtmlAttributeName("class")]
        public string Class { get; set; }

        [HtmlAttributeName("size")]
        public int Size { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!string.IsNullOrWhiteSpace(UserId))
            {
                output.TagName = "img";
                if (!string.IsNullOrWhiteSpace(Class))
                {
                    output.Attributes.Add("class", Class); 
                }

                if (!string.IsNullOrWhiteSpace(Alt))
                {
                    output.Attributes.Add("alt", Alt);
                }
                
                output.Attributes.Add("src", GetAvatarUrl(UserId, Size));
                output.TagMode = TagMode.SelfClosing;
            } 
        }

        private static string GetAvatarUrl(string key, int size)
        {
            var sizeArg = size > 0 ? $"?s={size}" : "";

            return $"http://gravatar.dev.api/av/{key}{sizeArg}";
        }
    }
}
