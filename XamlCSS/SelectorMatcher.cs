﻿using System.Linq;
using XamlCSS.CssParsing;
using XamlCSS.Dom;

namespace XamlCSS
{
    public class SelectorMatcher
    {
        public CssNodeType Type { get; protected set; }
        public string Text { get; protected set; }

        public SelectorMatcher(CssNodeType type, string text)
        {
            Type = type;
            Text = text;
        }

        virtual public MatchResult Match<TDependencyObject, TDependencyProperty>(StyleSheet styleSheet, ref IDomElement<TDependencyObject, TDependencyProperty> domElement, SelectorMatcher[] fragments, ref int currentIndex)
        {
            if (Type == CssNodeType.GeneralDescendantCombinator)
            {
                currentIndex--;
                var fragment = fragments[currentIndex];

                var current = domElement.LogicalParent;
                while (current != null)
                {
                    if (fragment.Match(styleSheet, ref current, fragments, ref currentIndex).IsSuccess)
                    {
                        domElement = current;
                        return MatchResult.Success;
                    }
                    current = current.LogicalParent;
                }
                return MatchResult.GeneralParentFailed;
            }

            else if (Type == CssNodeType.DirectDescendantCombinator)
            {
                var result = domElement.LogicalParent?.LogicalChildNodes.Contains(domElement) == true;
                domElement = domElement.LogicalParent;
                return result ? MatchResult.Success : MatchResult.DirectParentFailed;
            }

            else if (Type == CssNodeType.GeneralSiblingCombinator)
            {
                var thisIndex = domElement.LogicalParent?.LogicalChildNodes.IndexOf(domElement) ?? -1;

                if (thisIndex == 0)
                {
                    return MatchResult.ItemFailed;
                }

                currentIndex--;

                if ((domElement.LogicalParent?.LogicalChildNodes.Count > 0) == true)
                {
                    foreach (var sibling in domElement.LogicalParent.LogicalChildNodes.Take(thisIndex))
                    {
                        var refSibling = sibling;
                        if (fragments[currentIndex].Match(styleSheet, ref refSibling, fragments, ref currentIndex).IsSuccess)
                        {
                            domElement = sibling;
                            return MatchResult.Success;
                        }
                    }
                }

                return MatchResult.ItemFailed;
            }

            else if (Type == CssNodeType.DirectSiblingCombinator)
            {
                var thisIndex = domElement.LogicalParent?.LogicalChildNodes.IndexOf(domElement) ?? -1;

                if (thisIndex <= 0)
                {
                    return MatchResult.ItemFailed;
                }

                var sibling = domElement.LogicalParent?.LogicalChildNodes[thisIndex - 1];
                if (sibling == null)
                {
                    return MatchResult.ItemFailed;
                }
                currentIndex--;

                var result = fragments[currentIndex].Match(styleSheet, ref sibling, fragments, ref currentIndex);
                domElement = sibling;

                return result;
            }

            else if(Type == CssNodeType.PseudoSelector)
            {
                if (Text == ":visualtree")
                {
                    if (currentIndex == fragments.Length - 1)
                    {
                        currentIndex--;
                        if (currentIndex >= 0)
                            return fragments[currentIndex].Match(styleSheet, ref domElement, fragments, ref currentIndex);
                        return MatchResult.ItemFailed;
                    }
                    else
                    {
                        return GeneralVisualDescendantCombinator(styleSheet, ref domElement, fragments, ref currentIndex);
                    }
                }
            }

            return MatchResult.ItemFailed;
        }

        private static MatchResult GeneralVisualDescendantCombinator<TDependencyObject, TDependencyProperty>(StyleSheet styleSheet, ref IDomElement<TDependencyObject, TDependencyProperty> domElement, SelectorMatcher[] fragments, ref int currentIndex)
        {
            currentIndex--;
            var fragment = fragments[currentIndex];

            var current = domElement.Parent;
            while (current != null)
            {
                if (fragment.Match(styleSheet, ref current, fragments, ref currentIndex).IsSuccess)
                {
                    domElement = current;
                    return MatchResult.Success;
                }
                current = current.Parent;
            }
            return MatchResult.GeneralParentFailed;
        }
    }
}
