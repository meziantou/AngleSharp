﻿namespace AngleSharp.Dom
{
    using AngleSharp.Dom.Collections;
    using AngleSharp.Extensions;
    using AngleSharp.Html;
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents a node in the generated tree.
    /// </summary>
    [DebuggerStepThrough]
    internal class Node : EventTarget, INode, IEquatable<INode>
    {
        #region Fields

        readonly NodeType _type;
        readonly String _name;
        readonly NodeFlags _flags;

        WeakReference<Document> _owner;
        Url _baseUri;
        Node _parent;
        NodeList _children;

        #endregion

        #region ctor

        internal Node(Document owner, String name, NodeType type = NodeType.Element, NodeFlags flags = NodeFlags.None)
        {
            _owner = new WeakReference<Document>(owner);
            _name = name ?? String.Empty;
            _type = type;
            _children = this.CreateChildren();
            _flags = flags;
        }

        #endregion

        #region Public Properties

        public Boolean HasChildNodes
        {
            get { return _children.Length != 0; }
        }

        public String BaseUri
        {
            get 
            {
                var url = BaseUrl;
                return url != null ? url.Href : String.Empty;
            }
        }

        public Url BaseUrl
        {
            get
            {
                if (_baseUri != null)
                {
                    return _baseUri;
                }
                else if (_parent != null)
                {
                    return _parent.BaseUrl;
                }
                else
                {
                    var document = Owner;

                    if (document != null)
                    {
                        return document._baseUri ?? document.DocumentUrl;
                    }
                    else if (_type == NodeType.Document)
                    {
                        document = (Document)this;
                        return document.DocumentUrl;
                    }
                }

                return null;
            }
            set { _baseUri = value; }
        }

        public NodeType NodeType 
        {
            get { return _type; }
        }

        public virtual String NodeValue 
        {
            get { return null; }
            set { }
        }

        public virtual String TextContent
        {
            get { return null; }
            set { }
        }

        INode INode.PreviousSibling
        {
            get { return PreviousSibling; }
        }

        INode INode.NextSibling
        {
            get { return NextSibling; }
        }

        INode INode.FirstChild
        {
            get { return FirstChild; }
        }

        INode INode.LastChild
        {
            get { return LastChild; }
        }

        IDocument INode.Owner
        {
            get { return Owner; }
        }

        INode INode.Parent
        {
            get { return _parent; }
        }

        public IElement ParentElement
        {
            get { return _parent as IElement; }
        }

        INodeList INode.ChildNodes
        {
            get { return _children; }
        }

        public String NodeName
        {
            get { return _name; }
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the node immediately preceding this node's parent's list of
        /// nodes, null if the specified node is the first in that list.
        /// </summary>
        internal Node PreviousSibling
        {
            get
            {
                if (_parent != null)
                {
                    var n = _parent._children.Length;

                    for (var i = 1; i < n; i++)
                    {
                        if (Object.ReferenceEquals(_parent._children[i], this))
                        {
                            return _parent._children[i - 1];
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the node immediately following this node's parent's list of
        /// nodes, or null if the current node is the last node in that list.
        /// </summary>
        internal Node NextSibling
        {
            get
            {
                if (_parent != null)
                {
                    var n = _parent._children.Length - 1;

                    for (var i = 0; i < n; i++)
                    {
                        if (Object.ReferenceEquals(_parent._children[i], this))
                        {
                            return _parent._children[i + 1];
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the first child node of this node.
        /// </summary>
        internal Node FirstChild
        {
            get { return _children.Length > 0 ? _children[0] : null; }
        }

        /// <summary>
        /// Gets the last child node of this node.
        /// </summary>
        internal Node LastChild
        {
            get { return _children.Length > 0 ? _children[_children.Length - 1] : null; }
        }

        /// <summary>
        /// Gets the flags of this node.
        /// </summary>
        internal NodeFlags Flags
        {
            get { return _flags; }
        }

        /// <summary>
        /// Gets or sets the children of this node.
        /// </summary>
        internal NodeList ChildNodes
        {
            get { return _children; }
            set { _children = value; }
        }

        /// <summary>
        /// Gets the parent node of this node, which is either an Element node,
        /// a Document node, or a DocumentFragment node.
        /// </summary>
        internal Node Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        /// <summary>
        /// Gets the owner document of the node.
        /// </summary>
        internal Document Owner
        {
            get
            {
                var owner = default(Document);

                if (_type != NodeType.Document)
                {
                    _owner.TryGetTarget(out owner);
                }

                return owner;
            }
            set
            {
                var oldDocument = Owner;

                if (!Object.ReferenceEquals(oldDocument, value))
                {
                    _owner = new WeakReference<Document>(value);

                    for (var i = 0; i < _children.Length; i++)
                    {
                        _children[i].Owner = value;
                    }

                    if (oldDocument != null)
                    {
                        NodeIsAdopted(oldDocument);
                    }
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Appends the given characters to the node.
        /// </summary>
        /// <param name="s">The characters to append.</param>
        internal void AppendText(String s)
        {
            var lastChild = LastChild as TextNode;

            if (lastChild == null)
            {
                AddNode(new TextNode(Owner, s));
            }
            else
            {
                lastChild.Append(s);
            }
        }

        /// <summary>
        /// Inserts the given character in the node.
        /// </summary>
        /// <param name="index">The index where to insert.</param>
        /// <param name="s">The characters to append.</param>
        internal void InsertText(Int32 index, String s)
        {
            if (index > 0 && index <= _children.Length && _children[index - 1].NodeType == NodeType.Text)
            {
                var node = (IText)_children[index - 1];
                node.Append(s);
            }
            else if (index >= 0 && index < _children.Length && _children[index].NodeType == NodeType.Text)
            {
                var node = (IText)_children[index];
                node.Insert(0, s);
            }
            else
            {
                var node = new TextNode(Owner, s);
                InsertNode(index, node);
            }
        }

        #endregion

        #region Public Methods

        public INode AppendChild(INode child)
        {
            return this.PreInsert(child, null);
        }

        public INode InsertChild(Int32 index, INode child)
        {
            return this.PreInsert(child, _children[index]);
        }

        public INode InsertBefore(INode newElement, INode referenceElement)
        {
            return this.PreInsert(newElement, referenceElement);
        }

        public INode ReplaceChild(INode newChild, INode oldChild)
        {
            return this.ReplaceChild(newChild as Node, oldChild as Node, false);
        }

        public INode RemoveChild(INode child)
        {
            return this.PreRemove(child);
        }

        public virtual INode Clone(Boolean deep = true)
        {
            var node = new Node(Owner, _name, _type, _flags);
            CopyProperties(this, node, deep);
            return node;
        }

        public DocumentPositions CompareDocumentPosition(INode otherNode)
        {
            if (Object.ReferenceEquals(this, otherNode))
            {
                return DocumentPositions.Same;
            }
            else if (!Object.ReferenceEquals(Owner, otherNode.Owner))
            {
                var relative = otherNode.GetHashCode() > GetHashCode() ? DocumentPositions.Following : DocumentPositions.Preceding;
                return DocumentPositions.Disconnected | DocumentPositions.ImplementationSpecific | relative;
            }
            else if (otherNode.IsAncestorOf(this))
            {
                return DocumentPositions.Contains | DocumentPositions.Preceding;
            }
            else if (otherNode.IsDescendantOf(this))
            {
                return DocumentPositions.ContainedBy | DocumentPositions.Following;
            }
            else if (otherNode.IsPreceding(this))
            {
                return DocumentPositions.Preceding;
            }

            return DocumentPositions.Following;
        }

        public Boolean Contains(INode otherNode)
        {
            return this.IsInclusiveAncestorOf(otherNode);
        }

        public void Normalize()
        {
            for (var i = 0; i < _children.Length; i++)
            {
                var text = _children[i] as TextNode;

                if (text != null)
                {
                    var length = text.Length;

                    if (length == 0)
                    {
                        RemoveChild(text, false);
                        i--;
                    }
                    else
                    {
                        var sb = Pool.NewStringBuilder();
                        var sibling = text;
                        var end = i;
                        var owner = Owner;

                        while ((sibling = sibling.NextSibling as TextNode) != null)
                        {
                            sb.Append(sibling.Data);
                            end++;

                            owner.ForEachRange(m => m.Head == sibling, m => m.StartWith(text, length));
                            owner.ForEachRange(m => m.Tail == sibling, m => m.EndWith(text, length));
                            owner.ForEachRange(m => m.Head == sibling.Parent && m.Start == end, m => m.StartWith(text, length));
                            owner.ForEachRange(m => m.Tail == sibling.Parent && m.End == end, m => m.EndWith(text, length));

                            length += sibling.Length;
                        }

                        text.Replace(text.Length, 0, sb.ToPool());

                        for (var j = end; j > i; j--)
                        {
                            RemoveChild(_children[j], false);
                        }
                    }
                }
                else if (_children[i].HasChildNodes)
                {
                    _children[i].Normalize();
                }
            }
        }

        public String LookupNamespaceUri(String prefix)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                prefix = null;
            }

            return LocateNamespace(prefix);
        }

        public String LookupPrefix(String namespaceUri)
        {
            if (String.IsNullOrEmpty(namespaceUri))
            {
                return null;
            }

            return LocatePrefix(namespaceUri);
        }

        public Boolean IsDefaultNamespace(String namespaceUri)
        {
            if (String.IsNullOrEmpty(namespaceUri))
            {
                namespaceUri = null;
            }

            var defaultNamespace = LocateNamespace(null);
            return namespaceUri.Is(defaultNamespace);
        }

        public virtual Boolean Equals(INode otherNode)
        {
            if (BaseUri.Is(otherNode.BaseUri) && NodeName.Is(otherNode.NodeName) && ChildNodes.Length == otherNode.ChildNodes.Length)
            {
                for (var i = 0; i < _children.Length; i++)
                {
                    if (!_children[i].Equals(otherNode.ChildNodes[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// For more information, see:
        /// https://dom.spec.whatwg.org/#validate-and-extract
        /// </summary>
        protected static void GetPrefixAndLocalName(String qualifiedName, ref String namespaceUri, out String prefix, out String localName)
        {
            if (!qualifiedName.IsXmlName())
            {
                throw new DomException(DomError.InvalidCharacter);
            }
            else if (!qualifiedName.IsQualifiedName())
            {
                throw new DomException(DomError.Namespace);
            }

            if (String.IsNullOrEmpty(namespaceUri))
            {
                namespaceUri = null;
            }

            var index = qualifiedName.IndexOf(Symbols.Colon);

            if (index > 0)
            {
                prefix = qualifiedName.Substring(0, index);
                localName = qualifiedName.Substring(index + 1);
            }
            else
            {
                prefix = null;
                localName = qualifiedName;
            }

            if ((prefix != null && namespaceUri == null) ||
                (prefix.Is(Namespaces.XmlPrefix) && !namespaceUri.Is(Namespaces.XmlUri)) ||
                ((qualifiedName.Is(Namespaces.XmlNsPrefix) || prefix.Is(Namespaces.XmlNsPrefix)) && !namespaceUri.Is(Namespaces.XmlNsUri)) ||
                (namespaceUri.Is(Namespaces.XmlNsUri) && (!qualifiedName.Is(Namespaces.XmlNsPrefix) && !prefix.Is(Namespaces.XmlNsPrefix))))
            {
                throw new DomException(DomError.Namespace);
            }
        }

        /// <summary>
        /// Tries to locate the namespace of the given prefix.
        /// </summary>
        /// <param name="prefix">The prefix of the namespace.</param>
        /// <returns>The namespace for the prefix.</returns>
        protected virtual String LocateNamespace(String prefix)
        {
            if (_parent != null)
            {
                return _parent.LocateNamespace(prefix);
            }

            return null;
        }

        /// <summary>
        /// Tries to locate the prefix with the namespace.
        /// </summary>
        /// <param name="namespaceUri">
        /// The namespace assigned to the prefix.
        /// </param>
        /// <returns>The prefix for the namespace.</returns>
        protected virtual String LocatePrefix(String namespaceUri)
        {
            if (_parent != null)
            {
                return _parent.LocatePrefix(namespaceUri);
            }

            return null;
        }

        /// <summary>
        /// Adopts the current node for the provided document.
        /// </summary>
        /// <param name="document">The new owner of the node.</param>
        internal void ChangeOwner(Document document)
        {
            var oldDocument = Owner;

            if (_parent != null)
            {
                _parent.RemoveChild(this, false);
            }

            Owner = document;
            NodeIsAdopted(oldDocument);
        }

        internal void InsertNode(Int32 index, Node node)
        {
            node.Parent = this;
            _children.Insert(index, node);
        }

        internal void AddNode(Node node)
        {
            node.Parent = this;
            _children.Add(node);
        }

        internal void RemoveNode(Int32 index, Node node)
        {
            node.Parent = null;
            _children.RemoveAt(index);
        }

        /// <summary>
        /// Replaces all nodes with the given node, if any.
        /// </summary>
        /// <param name="node">The node to insert, if any.</param>
        /// <param name="suppressObservers">
        /// If mutation observers should be surpressed.
        /// </param>
        internal void ReplaceAll(Node node, Boolean suppressObservers)
        {
            var document = Owner;

            if (node != null)
            {
                document.AdoptNode(node);
            }

            var removedNodes = new NodeList(_children);
            var addedNodes = new NodeList();
            
            if (node != null)
            {
                if (node.NodeType == NodeType.DocumentFragment)
                {
                    addedNodes.AddRange(node._children);
                }
                else
                {
                    addedNodes.Add(node);
                }
            }

            for (int i = 0; i < removedNodes.Length; i++)
            {
                RemoveChild(removedNodes[i], true);
            }

            for (int i = 0; i < addedNodes.Length; i++)
            {
                InsertBefore(addedNodes[i], null, true);
            }

            if (!suppressObservers)
            {
                document.QueueMutation(MutationRecord.ChildList(
                    target: this,
                    addedNodes: addedNodes,
                    removedNodes: removedNodes));
            }
        }

        /// <summary>
        /// Inserts the specified node before a reference element as a child of
        /// the current node.
        /// </summary>
        /// <param name="newElement">The node to insert.</param>
        /// <param name="referenceElement">
        /// The node before which newElement is inserted. If referenceElement
        /// is null, newElement is inserted at the end of the list of child nodes.
        /// </param>
        /// <param name="suppressObservers">
        /// If mutation observers should be surpressed.
        /// </param>
        /// <returns>The inserted node.</returns>
        internal INode InsertBefore(Node newElement, Node referenceElement, Boolean suppressObservers)
        {
            var document = Owner;
            var count = newElement.NodeType == NodeType.DocumentFragment ? newElement.ChildNodes.Length : 1;

            if (referenceElement != null)
            {
                var childIndex = referenceElement.Index();
                document.ForEachRange(m => m.Head == this && m.Start > childIndex, m => m.StartWith(this, m.Start + count));
                document.ForEachRange(m => m.Tail == this && m.End > childIndex, m => m.EndWith(this, m.End + count));
            }

            if (newElement.NodeType == NodeType.Document || newElement.Contains(this))
            {
                throw new DomException(DomError.HierarchyRequest);
            }

            var addedNodes = new NodeList();
            var n = _children.Index(referenceElement);

            if (n == -1)
            {
                n = _children.Length;
            }
            
            if (newElement._type == NodeType.DocumentFragment)
            {
                var end = n;
                var start = n;

                while (newElement.HasChildNodes)
                {
                    var child = newElement.ChildNodes[0];
                    newElement.RemoveChild(child, true);
                    InsertNode(end, child);
                    end++;
                }

                while (start < end)
                {
                    var child = _children[start];
                    addedNodes.Add(child);
                    NodeIsInserted(child);
                    start++;
                }
            }
            else
            {
                addedNodes.Add(newElement);
                InsertNode(n, newElement);
                NodeIsInserted(newElement);
            }

            if (!suppressObservers)
            {
                document.QueueMutation(MutationRecord.ChildList(
                    target: this,
                    addedNodes: addedNodes,
                    previousSibling: _children[n - 1],
                    nextSibling: referenceElement));
            }

            return newElement;
        }

        /// <summary>
        /// Removes a child from the collection of children.
        /// </summary>
        /// <param name="node">The child to remove.</param>
        /// <param name="suppressObservers">
        /// If mutation observers should be surpressed.
        /// </param>
        internal void RemoveChild(Node node, Boolean suppressObservers)
        {
            var document = Owner;
            var index = _children.Index(node);

            document.ForEachRange(m => m.Head.IsInclusiveDescendantOf(node), m => m.StartWith(this, index));
            document.ForEachRange(m => m.Tail.IsInclusiveDescendantOf(node), m => m.EndWith(this, index));
            document.ForEachRange(m => m.Head == this && m.Start > index, m => m.StartWith(this, m.Start - 1));
            document.ForEachRange(m => m.Tail == this && m.End > index, m => m.EndWith(this, m.End - 1));

            var oldPreviousSibling = index > 0 ? _children[index - 1] : null;

            if (!suppressObservers)
            {
                var removedNodes = new NodeList();
                removedNodes.Add(node);

                document.QueueMutation(MutationRecord.ChildList(
                    target: this, 
                    removedNodes: removedNodes, 
                    previousSibling: oldPreviousSibling, 
                    nextSibling: node.NextSibling));

                document.AddTransientObserver(node);
            }

            RemoveNode(index, node);
            NodeIsRemoved(node, oldPreviousSibling);
        }

        /// <summary>
        /// Replaces one child node of the specified element with another.
        /// </summary>
        /// <param name="node">
        /// The new node to replace oldChild. If it already exists in the DOM,
        /// it is first removed.
        /// </param>
        /// <param name="child">The existing child to be replaced.</param>
        /// <param name="suppressObservers">
        /// If mutation observers should be surpressed.
        /// </param>
        /// <returns>
        /// The replaced node. This is the same node as oldChild.
        /// </returns>
        internal INode ReplaceChild(Node node, Node child, Boolean suppressObservers)
        {
            if (this.IsEndPoint() || node.IsHostIncludingInclusiveAncestor(this))
            {
                throw new DomException(DomError.HierarchyRequest);
            }
            else if (child.Parent != this)
            {
                throw new DomException(DomError.NotFound);
            }

            if (node.IsInsertable())
            {
                var parent = _parent as IDocument;
                var referenceChild = child.NextSibling;
                var document = Owner;
                var addedNodes = new NodeList();
                var removedNodes = new NodeList();

                if (parent != null)
                {
                    var forbidden = false;

                    switch (node._type)
                    {
                        case NodeType.DocumentType:
                            forbidden = parent.Doctype != child || child.IsPrecededByElement();
                            break;
                        case NodeType.Element:
                            forbidden = parent.DocumentElement != child || child.IsFollowedByDoctype();
                            break;
                        case NodeType.DocumentFragment:
                            var elements = node.GetElementCount();
                            forbidden = elements > 1 || node.HasTextNodes() || (elements == 1 && (parent.DocumentElement != child || child.IsFollowedByDoctype()));
                            break;
                    }

                    if (forbidden)
                    {
                        throw new DomException(DomError.HierarchyRequest);
                    }
                }

                if (referenceChild == node)
                {
                    referenceChild = node.NextSibling;
                }

                document.AdoptNode(node);
                RemoveChild(child, true);
                InsertBefore(node, referenceChild, true);
                removedNodes.Add(child);

                if (node._type == NodeType.DocumentFragment)
                {
                    addedNodes.AddRange(node._children);
                }
                else
                {
                    addedNodes.Add(node);
                }

                if (!suppressObservers)
                {
                    document.QueueMutation(MutationRecord.ChildList(
                        target: this,
                        addedNodes: addedNodes,
                        removedNodes: removedNodes,
                        previousSibling: child.PreviousSibling,
                        nextSibling: referenceChild));
                }

                return child;
            }
            
            throw new DomException(DomError.HierarchyRequest);
        }

        internal virtual void NodeIsAdopted(Document oldDocument)
        {
            //Run any adopting steps defined for node in other applicable
            //specifications and pass node and oldDocument as parameters.
        }

        internal virtual void NodeIsInserted(Node newNode)
        {
            //Specifications may define insertion steps for all or some nodes.
        }

        internal virtual void NodeIsRemoved(Node removedNode, Node oldPreviousSibling)
        {
            //Specifications may define removing steps for all or some nodes.
        }

        /// <summary>
        /// Copies all (Node) properties of the source to the target.
        /// </summary>
        /// <param name="source">The source node.</param>
        /// <param name="target">The target node.</param>
        /// <param name="deep">Is a deep-copy required?</param>
        static protected void CopyProperties(Node source, Node target, Boolean deep)
        {
            target._baseUri = source._baseUri;

            if (deep)
            {
                foreach (var child in source._children)
                {
                    target.AddNode((Node)child.Clone(true));
                }
            }
        }

        public String ToHtml()
        {
            return ToHtml(HtmlMarkupFormatter.Instance);
        }

        public virtual String ToHtml(IMarkupFormatter formatter)
        {
            return TextContent;
        }

        #endregion
    }
}
