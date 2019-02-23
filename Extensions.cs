using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace SVN.FritzBoxApi
{
    internal static class Extensions
    {
        private static XElement GetElement(this XElement element, string key)
        {
            foreach (var child in element.Elements().Where(x => x.Name == key))
            {
                return child;
            }
            return null;
        }

        public static IEnumerable<XElement> GetChilds(this XDocument document, string key)
        {
            var root = document.FirstNode as XElement;
            var element = root.GetElement(key);

            if (element is null)
            {
                yield break;
            }

            foreach (var child in element.Elements())
            {
                yield return child;
            }
        }

        public static T GetValue<T>(this XElement element)
        {
            return (T)Convert.ChangeType(element.Value, typeof(T));
        }

        public static T GetValue<T>(this XDocument document, string key)
        {
            var root = document.FirstNode as XElement;
            var element = root.GetElement(key);

            if (element is null)
            {
                return default(T);
            }

            return element.GetValue<T>();
        }
    }
}