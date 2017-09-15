using System;
using System.Drawing;
using System.Globalization;
using System.Xml.Linq;

namespace TfsInfoService.Utilities
{
    public static class BadgeGenerator
    {
        private static readonly XNamespace s_namespace = "http://www.w3.org/2000/svg";

        public static XDocument CreateSvgBadge(
            string titleText, string titleForeColor, string titleBackColor,
            string valueText, string valueForeColor, string valueBackColor)
        {
            using (var font = new Font("Segoe UI", 13f, GraphicsUnit.Pixel))
            using (var graphics = Graphics.FromImage(new Bitmap(1, 1)))
            {
                var sizeLeftText = graphics.MeasureString(titleText, font);
                var sizeRightText = graphics.MeasureString(valueText, font);

                double totalTextWidth = sizeLeftText.Width + sizeRightText.Width;
                double height = Math.Max(sizeLeftText.Height, sizeRightText.Height);
                double paddedLeftWidth = sizeLeftText.Width + 1f;
                double width = totalTextWidth - sizeLeftText.Width;
                double leftTextX = paddedLeftWidth / 2.0;
                double rightTextX = (totalTextWidth - paddedLeftWidth) / 2.0 + paddedLeftWidth;

                return new XDocument(new object[]
                {
                    new XElement(s_namespace + "svg", new object[]
                    {
                        new XAttribute("width", totalTextWidth.ToString("0.0", CultureInfo.InvariantCulture)),
                        new XAttribute("height", height.ToString("0.0", CultureInfo.InvariantCulture)),
                        Rectangle(totalTextWidth, height, titleBackColor),
                        Rectangle(paddedLeftWidth, width, height, valueBackColor),
                        new XElement(s_namespace + "g", new object[]
                        {
                            new XAttribute("fill", "#fff"),
                            new XAttribute("text-anchor", "middle"),
                            new XAttribute("font-family", "Segoe UI, Helvetica Neue, Helvetica, Arial, Verdana"),
                            new XAttribute("font-size", "12"),
                            Text(leftTextX, 14.0, titleText, titleForeColor),
                            Text(rightTextX, 14.0, valueText, valueForeColor)
                        })
                    })
                });
            }
        }

        private static XElement Rectangle(double width, double height, string fillColor)
        {
            return new XElement(s_namespace + "rect",
                new XAttribute("width", width.ToString("0.0", CultureInfo.InvariantCulture)),
                new XAttribute("height", height.ToString("0.0", CultureInfo.InvariantCulture)),
                new XAttribute("fill", fillColor));
        }

        private static XElement Rectangle(double x, double width, double height, string fillColor)
        {
            XElement element = Rectangle(width, height, fillColor);
            element.SetAttributeValue("x", x.ToString("0.0", CultureInfo.InvariantCulture));
            return element;
        }

        private static XElement Text(double x, double y, string text)
        {
            return new XElement(s_namespace + "text",
                new XAttribute("x", x.ToString("0.0", CultureInfo.InvariantCulture)),
                new XAttribute("y", y.ToString("0.0", CultureInfo.InvariantCulture)),
                text);
        }

        private static XElement Text(double x, double y, string text, string fillColor)
        {
            var element = Text(x, y, text);
            element.SetAttributeValue("fill", fillColor);
            return element;
        }
    }
}