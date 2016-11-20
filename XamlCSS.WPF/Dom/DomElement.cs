﻿using System;
using System.Collections.Generic;
using System.Windows;
using AngleSharp.Dom;
using XamlCSS.Dom;
using System.Windows.Media;

namespace XamlCSS.WPF.Dom
{
    public abstract class DomElement : DomElementBase<DependencyObject, DependencyProperty>
    {
        public DomElement(DependencyObject dependencyObject, IElement parent)
            : base(dependencyObject, parent)
        {
            RegisterChildrenChangeHandler();
        }
        public DomElement(DependencyObject dependencyObject, Func<DependencyObject, IElement> getParent)
            : base(dependencyObject, getParent)
        {
            RegisterChildrenChangeHandler();
        }

        private void RegisterChildrenChangeHandler()
        {
            LoadedDetectionHelper.SubTreeAdded += ElementAdded;
            LoadedDetectionHelper.SubTreeRemoved += ElementAdded;
        }

        public new void Dispose()
        {
            UnregisterChildrenChangeHandler();

            base.Dispose();
        }

        private void UnregisterChildrenChangeHandler()
        {
            LoadedDetectionHelper.SubTreeAdded -= ElementAdded;
            LoadedDetectionHelper.SubTreeRemoved -= ElementAdded;
        }

        private void ElementAdded(object sender, EventArgs e)
        {
            var parentElement = (sender as FrameworkElement)?.Parent ??
                (sender as FrameworkContentElement)?.Parent;

            if (parentElement == dependencyObject)
            {
                this.ResetChildren();
            }
        }

        protected override IHtmlCollection<IElement> CreateCollection(IEnumerable<IElement> list)
        {
            return new ElementCollection(list);
        }
        protected override INamedNodeMap CreateNamedNodeMap(DependencyObject dependencyObject)
        {
            return new NamedNodeMap(dependencyObject);
        }

        protected override IHtmlCollection<IElement> GetChildElements(DependencyObject dependencyObject)
        {
            return new ElementCollection(this);
        }
        protected override INodeList GetChildNodes(DependencyObject dependencyObject)
        {
            return new NamedNodeList(this);
        }
        protected override INodeList CreateNodeList(IEnumerable<INode> nodes)
        {
            return new NamedNodeList(nodes);
        }
        protected override ITokenList GetClassList(DependencyObject dependencyObject)
        {
            var list = new TokenList();

            var classNames = Css.GetClass(dependencyObject)?.Split(classSplitter, StringSplitOptions.RemoveEmptyEntries);
            if (classNames?.Length > 0)
            {
                list.AddRange(classNames);
            }

            return list;
        }
        protected override string GetId(DependencyObject dependencyObject)
        {
            if (dependencyObject is FrameworkElement)
            {
                return dependencyObject.ReadLocalValue(FrameworkElement.NameProperty) as string;
            }

            return dependencyObject.ReadLocalValue(FrameworkContentElement.NameProperty) as string;
        }
    }
}
