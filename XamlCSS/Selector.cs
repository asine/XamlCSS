﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using XamlCSS.CssParsing;
using XamlCSS.Dom;

namespace XamlCSS
{
    [DebuggerDisplay("{Value}")]
    public class Selector : ISelector
    {
        public Selector()
        {

        }

        public Selector(string selectorString)
            : this(selectorString, null)
        {

        }

        internal Selector(string selectorString, CssNode selectorAst)
        {
            if (selectorAst != null)
            {
                this.selectorAst = selectorAst;
                this.val = selectorString;
                CalculateSpecificity(selectorString);
            }
            else
            {
                this.Value = selectorString;
            }
        }

        internal Selector(IEnumerable<SelectorMatcher> fragments)
        {
            this.fragments = fragments.ToArray();
            this.fragmentLength = this.fragments.Length;
        }

        protected string val;
        private CssNode selectorAst;

        public string Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;

                CalculateSpecificity(value);

                GetFragments(val);
            }
        }

        private void CalculateSpecificity(string value)
        {
            var result = SpecificityCalculator.Calculate(value);

            IdSpecificity = result.IdSpecificity;
            ClassSpecificity = result.ClassSpecificity;
            SimpleSpecificity = result.SimpleSpecificity;
        }

        private void GetFragments(string val)
        {
            var tokenizer = new Tokenizer();
            var selectorTokens = Tokenizer.Tokenize(val);
            var a = new AstGenerator();
            var ast = a.GetAst(selectorTokens);

            selectorAst = ast.Root.Children.First().Children.First();

            SetFragmentsFromAst();
        }

        private void SetFragmentsFromAst()
        {
            fragments = selectorAst.Children.SelectMany(x =>
            {
                if (x.Type == CssNodeType.DirectDescendantCombinator ||
                    x.Type == CssNodeType.GeneralDescendantCombinator ||
                    x.Type == CssNodeType.DirectSiblingCombinator ||
                    x.Type == CssNodeType.GeneralSiblingCombinator)
                {
                    return new[] { new SelectorMatcherFactory().Create(x.Type, x.Text) };
                }
                return x.Children.Select(y => new SelectorMatcherFactory().Create(y.Type, y.Text));
            })
            .ToArray();
            fragmentLength = fragments.Length;
        }

        public int SimpleSpecificity { get; private set; }
        public int ClassSpecificity { get; private set; }
        public int IdSpecificity { get; private set; }

        internal SelectorMatcher[] fragments;
        private int fragmentLength;

        public string Specificity
        {
            get
            {
                if (IdSpecificity > 0)
                {
                    return $"{IdSpecificity},{ClassSpecificity},{SimpleSpecificity}";
                }
                else if (ClassSpecificity > 0)
                {
                    return $"{ClassSpecificity},{SimpleSpecificity}";
                }
                else
                {
                    return $"{SimpleSpecificity}";
                }
            }
        }

        public static bool operator <(Selector e1, Selector e2)
        {
            if (e1.IdSpecificity < e2.IdSpecificity)
                return true;
            if (e1.ClassSpecificity < e2.ClassSpecificity)
                return true;
            if (e1.SimpleSpecificity < e2.SimpleSpecificity)
                return true;

            return false;
        }

        public static bool operator >(Selector e1, Selector e2)
        {
            if (e1.IdSpecificity > e2.IdSpecificity)
                return true;
            if (e1.ClassSpecificity > e2.ClassSpecificity)
                return true;
            if (e1.SimpleSpecificity > e2.SimpleSpecificity)
                return true;

            return false;
        }

        public MatchResult Match<TDependencyObject, TDependencyProperty>(StyleSheet styleSheet, IDomElement<TDependencyObject, TDependencyProperty> domElement)
            where TDependencyObject : class
        {
            for (var i = fragmentLength - 1; i >= 0; i--)
            {
                var fragment = fragments[i];

                var match = fragment.Match(styleSheet, ref domElement, fragments, ref i);
                if (!match.IsSuccess)
                {
                    return match;
                }
            }

            return MatchResult.Success;
        }

        public bool StartOnVisualTree()
        {
            if (fragments[fragmentLength - 1].Type == CssNodeType.PseudoSelector &&
                fragments[fragmentLength - 1].Text == ":visualtree")
            {
                return true;
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Selector;
            if (ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return other.Value == this.Value;
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }
    }
}
