using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.IO;

using Sax.Net;
using Sax.Net.Ext;
using Sax.Net.Helpers;

/// <summary>
///     Filter to write an XML document from a SAX event stream.
///     <para>
///         This class can be used by itself or as part of a SAX event
///         stream: it takes as input a series of SAX2 ContentHandler
///         events and uses the information in those events to write
///         an XML document.  Since this class is a filter, it can also
///         pass the events on down a filter chain for further processing
///         (you can use the XMLWriter to take a snapshot of the current
///         state at any point in a filter chain), and it can be
///         used directly as a ContentHandler for a SAX2 XMLReader.
///     </para>
///     <para>
///         The client creates a document by invoking the methods for
///         standard SAX2 events, always beginning with the
///         <see cref="StartDocument" /> method and ending with
///         the <see cref="EndDocument" /> method.  There are convenience
///         methods provided so that clients to not have to create empty
///         attribute lists or provide empty strings as parameters; for
///         example, the method invocation
///     </para>
///     <code>
/// w.startElement("foo");
/// </code>
///     <para>is equivalent to the regular SAX2 ContentHandler method</para>
///     <code>
/// w.startElement("", "foo", "", new AttributesImpl());
/// </code>
///     <para>
///         Except that it is more efficient because it does not allocate
///         a new empty attribute list each time.  The following code will send
///         a simple XML document to standard output:
///     </para>
///     <code>
/// XMLWriter w = new XMLWriter();
/// w.startDocument();
/// w.startElement("greeting");
/// w.characters("Hello, world!");
/// w.endElement("greeting");
/// w.endDocument();
/// </code>
///     <para>The resulting document will look like this:</para>
///     <code>
/// &lt;?xml version="1.0" standalone="yes"?>
/// &lt;greeting>Hello, world!&lt;/greeting>
/// </code>
///     <para>
///         In fact, there is an even simpler convenience method,
///         <c>dataElement</c>, designed for writing elements that
///         contain only character data, so the code to generate the
///         document could be shortened to
///     </para>
///     <code>
/// XMLWriter w = new XMLWriter();
/// w.startDocument();
/// w.dataElement("greeting", "Hello, world!");
/// w.endDocument();
/// </code>
///     <h2>Whitespace</h2>
///     <para>
///         According to the XML Recommendation, <em>all</em> whitespace
///         in an XML document is potentially significant to an application,
///         so this class never adds newlines or indentation.  If you
///         insert three elements in a row, as in
///     </para>
///     <code>
/// w.dataElement("item", "1");
/// w.dataElement("item", "2");
/// w.dataElement("item", "3");
/// </code>
///     <para>you will end up with</para>
///     <code>
/// &lt;item>1&lt;/item>&lt;item>3&lt;/item>&lt;item>3&lt;/item>
/// </code>
///     <para>
///         You need to invoke one of the <c>characters</c> methods
///         explicitly to add newlines or indentation.  Alternatively, you
///         can use <see cref="com.megginson.sax.DataWriter DataWriter" />, which
///         is derived from this class -- it is optimized for writing
///         purely data-oriented (or field-oriented) XML, and does automatic
///         linebreaks and indentation (but does not support mixed content
///         properly).
///     </para>
///     <h2>Namespace Support</h2>
///     <para>
///         The writer contains extensive support for XML Namespaces, so that
///         a client application does not have to keep track of prefixes and
///         supply <c>xmlns</c> attributes.  By default, the XML writer will
///         generate Namespace declarations in the form _NS1, _NS2, etc., wherever
///         they are needed, as in the following example:
///     </para>
///     <code>
/// w.startDocument();
/// w.emptyElement("http://www.foo.com/ns/", "foo");
/// w.endDocument();
/// </code>
///     <para>The resulting document will look like this:</para>
///     <code>
/// &lt;?xml version="1.0" standalone="yes"?>
/// &lt;_NS1:foo xmlns:_NS1="http://www.foo.com/ns/"/>
/// </code>
///     <para>
///         In many cases, document authors will prefer to choose their
///         own prefixes rather than using the (ugly) default names.  The
///         XML writer allows two methods for selecting prefixes:
///     </para>
///     <ol>
///         <li>the qualified name</li>
///         <li>the <see cref="Prefix" /> property.</li>
///     </ol>
///     <para>
///         Whenever the XML writer finds a new Namespace URI, it checks
///         to see if a qualified (prefixed) name is also available; if so
///         it attempts to use the name's prefix (as long as the prefix is
///         not already in use for another Namespace URI).
///     </para>
///     <para>
///         Before writing a document, the client can also pre-map a prefix
///         to a Namespace URI with the setPrefix method:
///     </para>
///     <code>
/// w.setPrefix("http://www.foo.com/ns/", "foo");
/// w.startDocument();
/// w.emptyElement("http://www.foo.com/ns/", "foo");
/// w.endDocument();
/// </code>
///     <para>The resulting document will look like this:</para>
///     <code>
/// &lt;?xml version="1.0" standalone="yes"?>
/// &lt;foo:foo xmlns:foo="http://www.foo.com/ns/"/>
/// </code>
///     <para>The default Namespace simply uses an empty string as the prefix:</para>
///     <code>
/// w.setPrefix("http://www.foo.com/ns/", "");
/// w.startDocument();
/// w.emptyElement("http://www.foo.com/ns/", "foo");
/// w.endDocument();
/// </code>
///     <para>The resulting document will look like this:</para>
///     <code>
/// &lt;?xml version="1.0" standalone="yes"?>
/// &lt;foo xmlns="http://www.foo.com/ns/"/>
/// </code>
///     <para>
///         By default, the XML writer will not declare a Namespace until
///         it is actually used.  Sometimes, this approach will create
///         a large number of Namespace declarations, as in the following
///         example:
///     </para>
///     <code>
/// &lt;xml version="1.0" standalone="yes"?>
/// &lt;rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
/// &lt;rdf:Description about="http://www.foo.com/ids/books/12345">
/// &lt;dc:title xmlns:dc="http://www.purl.org/dc/">A Dark Night&lt;/dc:title>
/// &lt;dc:creator xmlns:dc="http://www.purl.org/dc/">Jane Smith&lt;/dc:title>
/// &lt;dc:date xmlns:dc="http://www.purl.org/dc/">2000-09-09&lt;/dc:title>
/// &lt;/rdf:Description>
/// &lt;/rdf:RDF>
/// </code>
///     <para>
///         The "rdf" prefix is declared only once, because the RDF Namespace
///         is used by the root element and can be inherited by all of its
///         descendants; the "dc" prefix, on the other hand, is declared three
///         times, because no higher element uses the Namespace.  To solve this
///         problem, you can instruct the XML writer to predeclare Namespaces
///         on the root element even if they are not used there:
///     </para>
///     <code>
/// w.forceNSDecl("http://www.purl.org/dc/");
/// </code>
///     <para>
///         Now, the "dc" prefix will be declared on the root element even
///         though it's not needed there, and can be inherited by its
///         descendants:
///     </para>
///     <code>
/// &lt;xml version="1.0" standalone="yes"?>
/// &lt;rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"
/// xmlns:dc="http://www.purl.org/dc/">
/// &lt;rdf:Description about="http://www.foo.com/ids/books/12345">
/// &lt;dc:title>A Dark Night&lt;/dc:title>
/// &lt;dc:creator>Jane Smith&lt;/dc:title>
/// &lt;dc:date>2000-09-09&lt;/dc:title>
/// &lt;/rdf:Description>
/// &lt;/rdf:RDF>
/// </code>
///     <para>
///         This approach is also useful for declaring Namespace prefixes
///         that be used by qualified names appearing in attribute values or
///         character data.
///     </para>
/// </summary>
/// @author David Megginson, david@megginson.com
/// @version 0.2
/// <seealso cref="IXmlFilter" />
/// <seealso cref="IContentHandler" />
public class XmlWriter : XmlFilter, ILexicalHandler {
  public const string CDATA_SECTION_ELEMENTS = "cdata-section-elements";
  public const string DOCTYPE_PUBLIC = "doctype-public";
  public const string DOCTYPE_SYSTEM = "doctype-system";
  public const string ENCODING = "encoding";
  public const string INDENT = "indent"; // currently ignored
  public const string MEDIA_TYPE = "media-type"; // currently ignored
  public const string METHOD = "method"; // currently html or xml
  public const string OMIT_XML_DECLARATION = "omit-xml-declaration";
  public const string STANDALONE = "standalone"; // currently ignored
  public const string VERSION = "version";
  private readonly IAttributes _emptyAtts = new Attributes();
  private readonly string[] _bools = {
    "checked",
    "compact",
    "declare",
    "defer",
    "disabled",
    "ismap",
    "multiple",
    "nohref",
    "noresize",
    "noshade",
    "nowrap",
    "readonly",
    "selected"
  };
  private bool _cdataElement;
  private Hashtable _doneDeclTable;
  private int _elementLevel;
  private bool _forceDtd;
  private Hashtable _forcedDeclTable;
  private bool _hasOutputDtd;
  private bool _htmlMode;
  private NamespaceSupport _nsSupport;
  private TextWriter _output;
  private string _outputEncoding = "";
  private string _overridePublic;
  private string _overrideSystem;
  private int _prefixCounter;
  private Hashtable _prefixTable;
  private string _standalone;
  private bool _unicodeMode;
  private string _version;
  private NameValueCollection _outputProperties;

  /// <summary>
  ///     Create a new XML writer.
  ///     <para>Write to standard output.</para>
  /// </summary>
  public XmlWriter() {
    Init(null);
  }

  /// <summary>
  ///     Create a new XML writer.
  ///     <para>Write to the writer provided.</para>
  /// </summary>
  /// <param name="writer">
  ///     The output destination, or null to use standard
  ///     output.
  /// </param>
  public XmlWriter(TextWriter writer) {
    Init(writer);
  }

  /// <summary>
  ///     Create a new XML writer.
  ///     <para>Use the specified XML reader as the parent.</para>
  /// </summary>
  /// <param name="xmlreader">
  ///     The parent in the filter chain, or null
  ///     for no parent.
  /// </param>
  public XmlWriter(IXmlReader xmlreader) : base(xmlreader) {
    Init(null);
  }

  /// <summary>
  ///     Create a new XML writer.
  ///     <para>
  ///         Use the specified XML reader as the parent, and write
  ///         to the specified writer.
  ///     </para>
  /// </summary>
  /// <param name="xmlreader">
  ///     The parent in the filter chain, or null
  ///     for no parent.
  /// </param>
  /// <param name="writer">
  ///     The output destination, or null to use standard
  ///     output.
  /// </param>
  public XmlWriter(IXmlReader xmlreader, TextWriter writer) : base(xmlreader) {
    Init(writer);
  }

  public void Comment(char[] ch, int start, int length) {
    Write("<!--");
    for (int i = start; i < start + length; i++) {
      Write(ch[i]);
      if (ch[i] == '-' && i + 1 <= start + length && ch[i + 1] == '-') {
        Write(' ');
      }
    }
    Write("-->");
  }

  public void EndCDATA() {
  }

  public void EndDTD() {
  }

  public void EndEntity(string name) {
  }

  public void StartCDATA() {
  }

  public void StartDTD(string name, string publicid, string systemid) {
    if (name == null) {
      return; // can't cope
    }
    if (_hasOutputDtd) {
      return; // only one DTD
    }
    _hasOutputDtd = true;
    Write("<!DOCTYPE ");
    Write(name);
    if (systemid == null) {
      systemid = "";
    }
    if (_overrideSystem != null) {
      systemid = _overrideSystem;
    }
    char sysquote = (systemid.IndexOf('"') != -1) ? '\'' : '"';
    if (_overridePublic != null) {
      publicid = _overridePublic;
    }
    if (!(publicid == null || "".Equals(publicid))) {
      char pubquote = (publicid.IndexOf('"') != -1) ? '\'' : '"';
      Write(" PUBLIC ");
      Write(pubquote);
      Write(publicid);
      Write(pubquote);
      Write(' ');
    } else {
      Write(" SYSTEM ");
    }
    Write(sysquote);
    Write(systemid);
    Write(sysquote);
    Write(">\n");
  }

  public void StartEntity(string name) {
  }

  /// <summary>
  ///     Internal initialization method.
  ///     <para>All of the public constructors invoke this method.</para>
  /// </summary>
  /// <param name="writer">
  ///     The output destination, or null to use
  ///     standard output.
  /// </param>
  private void Init(TextWriter writer) {
    SetOutput(writer);
    _nsSupport = new NamespaceSupport();
    _prefixTable = new Hashtable();
    _forcedDeclTable = new Hashtable();
    _doneDeclTable = new Hashtable();
    _outputProperties = ConfigurationManager.AppSettings;
  }

  /// <summary>
  ///     Reset the writer.
  ///     <para>
  ///         This method is especially useful if the writer throws an
  ///         exception before it is finished, and you want to reuse the
  ///         writer for a new document.  It is usually a good idea to
  ///         invoke <see cref="Flush" /> before resetting the writer,
  ///         to make sure that no output is lost.
  ///     </para>
  ///     <para>
  ///         This method is invoked automatically by the
  ///         <see cref="StartDocument" /> method before writing
  ///         a new document.
  ///     </para>
  ///     <para>
  ///         <strong>Note:</strong> this method will <em>not</em>
  ///         clear the prefix or URI information in the writer or
  ///         the selected output writer.
  ///     </para>
  /// </summary>
  /// <seealso cref="Flush" />
  public void Reset() {
    _elementLevel = 0;
    _prefixCounter = 0;
    _nsSupport.Reset();
  }

  /// <summary>
  ///     Flush the output.
  ///     <para>
  ///         This method flushes the output stream.  It is especially useful
  ///         when you need to make certain that the entire document has
  ///         been written to output but do not want to close the output
  ///         stream.
  ///     </para>
  ///     <para>
  ///         This method is invoked automatically by the
  ///         <see cref="EndDocument" /> method after writing a
  ///         document.
  ///     </para>
  /// </summary>
  /// <seealso cref="Reset" />
  public void Flush() {
    _output.Flush();
  }

  /// <summary>
  ///     Set a new output destination for the document.
  /// </summary>
  /// <param name="writer">
  ///     The output destination, or null to use
  ///     standard output.
  /// </param>
  /// <seealso cref="Flush" />
  public void SetOutput(TextWriter writer) {
    if (writer == null) {
      _output = new StreamWriter(Console.OpenStandardOutput());
    } else {
      _output = writer;
    }
  }

  /// <summary>
  ///     Specify a preferred prefix for a Namespace URI.
  ///     <para>
  ///         Note that this method does not actually force the Namespace
  ///         to be declared; to do that, use the <see cref="ForceNSDecl(string)" />
  ///         method as well.
  ///     </para>
  /// </summary>
  /// <param name="uri">
  ///     The Namespace URI.
  /// </param>
  /// <param name="prefix">
  ///     The preferred prefix, or "" to select
  ///     the default Namespace.
  /// </param>
  /// <seealso cref="GetPrefix" />
  /// <seealso cref="ForceNSDecl(string)" />
  /// <seealso cref="ForceNSDecl(string,string)" />
  public void SetPrefix(string uri, string prefix) {
    _prefixTable[uri] = prefix;
  }

  /// <summary>
  ///     Get the current or preferred prefix for a Namespace URI.
  /// </summary>
  /// <param name="uri">The Namespace URI.</param>
  /// <returns>The preferred prefix, or "" for the default Namespace.</returns>
  /// <seealso cref="SetPrefix" />
  public string GetPrefix(string uri) {
    return (string)(_prefixTable.ContainsKey(uri) ? _prefixTable[uri] : string.Empty);
  }

  /// <summary>
  ///     Force a Namespace to be declared on the root element.
  ///     <para>
  ///         By default, the XMLWriter will declare only the Namespaces
  ///         needed for an element; as a result, a Namespace may be
  ///         declared many places in a document if it is not used on the
  ///         root element.
  ///     </para>
  ///     <para>
  ///         This method forces a Namespace to be declared on the root
  ///         element even if it is not used there, and reduces the number
  ///         of xmlns attributes in the document.
  ///     </para>
  /// </summary>
  /// <param name="uri">
  ///     The Namespace URI to declare.
  /// </param>
  /// <seealso cref="ForceNSDecl(string,string)" />
  /// <seealso cref="SetPrefix" />
  public void ForceNSDecl(string uri) {
    _forcedDeclTable[uri] = true;
  }

  /// <summary>
  ///     Force a Namespace declaration with a preferred prefix.
  ///     <para>
  ///         This is a convenience method that invokes <see cref="SetPrefix" />
  ///         then <see cref="ForceNSDecl(string)" />.
  ///     </para>
  /// </summary>
  /// <param name="uri">
  ///     The Namespace URI to declare on the root element.
  /// </param>
  /// <param name="prefix">
  ///     The preferred prefix for the Namespace, or ""
  ///     for the default Namespace.
  /// </param>
  /// <seealso cref="SetPrefix" />
  /// <seealso cref="ForceNSDecl" />
  public void ForceNSDecl(string uri, string prefix) {
    SetPrefix(uri, prefix);
    ForceNSDecl(uri);
  }

  ////////////////////////////////////////////////////////////////////
  // Methods from org.xml.sax.ContentHandler.
  ////////////////////////////////////////////////////////////////////

  /// <summary>
  ///     Write the XML declaration at the beginning of the document.
  ///     Pass the event on down the filter chain for further processing.
  /// </summary>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the XML declaration, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="IContentHandler.StartDocument" />
  public override void StartDocument() {
    Reset();
    if (!("yes".Equals(_outputProperties[OMIT_XML_DECLARATION] ?? "no"))) {
      Write("<?xml");
      if (_version == null) {
        Write(" version=\"1.0\"");
      } else {
        Write(" version=\"");
        Write(_version);
        Write("\"");
      }
      if (false == string.IsNullOrEmpty(_outputEncoding)) {
        Write(" encoding=\"");
        Write(_outputEncoding);
        Write("\"");
      }
      if (_standalone == null) {
        Write(" standalone=\"yes\"?>\n");
      } else {
        Write(" standalone=\"");
        Write(_standalone);
        Write("\"");
      }
    }
    base.StartDocument();
  }

  /// <summary>
  ///     Write a newline at the end of the document.
  ///     Pass the event on down the filter chain for further processing.
  /// </summary>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the newline, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="IContentHandler.EndDocument" />
  public override void EndDocument() {
    Write('\n');
    base.EndDocument();
    try {
      Flush();
    } catch (IOException e) {
      throw new SAXException(e.Message, e);
    }
  }

  /// <summary>
  ///     Write a start tag.
  ///     Pass the event on down the filter chain for further processing.
  /// </summary>
  /// <param name="uri">
  ///     The Namespace URI, or the empty string if none
  ///     is available.
  /// </param>
  /// <param name="localName">
  ///     The element's local (unprefixed) name (required).
  /// </param>
  /// <param name="qName">
  ///     The element's qualified (prefixed) name, or the
  ///     empty string is none is available.  This method will
  ///     use the qName as a template for generating a prefix
  ///     if necessary, but it is not guaranteed to use the
  ///     same qName.
  /// </param>
  /// <param name="atts">
  ///     The element's attribute list (must not be null).
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the start tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="IContentHandler.StartElement" />
  public override void StartElement(string uri, string localName, string qName, IAttributes atts) {
    _elementLevel++;
    _nsSupport.PushContext();
    if (_forceDtd && !_hasOutputDtd) {
      StartDTD(localName ?? qName, "", "");
    }
    Write('<');
    WriteName(uri, localName, qName, true);
    WriteAttributes(atts);
    if (_elementLevel == 1) {
      ForceNSDecls();
    }
    WriteNSDecls();
    Write('>');
    //	System.out.println("%%%% startElement [" + qName + "] htmlMode = " + htmlMode);
    if (_htmlMode && (qName.Equals("script") || qName.Equals("style"))) {
      _cdataElement = true;
      //		System.out.println("%%%% CDATA element");
    }
    base.StartElement(uri, localName, qName, atts);
  }

  /// <summary>
  ///     Write an end tag.
  ///     Pass the event on down the filter chain for further processing.
  /// </summary>
  /// <param name="uri">
  ///     The Namespace URI, or the empty string if none
  ///     is available.
  /// </param>
  /// <param name="localName">
  ///     The element's local (unprefixed) name (required).
  /// </param>
  /// <param name="qName">
  ///     The element's qualified (prefixed) name, or the
  ///     empty string is none is available.  This method will
  ///     use the qName as a template for generating a prefix
  ///     if necessary, but it is not guaranteed to use the
  ///     same qName.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the end tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="IContentHandler.EndElement" />
  public override void EndElement(string uri, string localName, string qName) {
    if (
      !(_htmlMode && (uri.Equals("http://www.w3.org/1999/xhtml") || uri.Equals(""))
        && (qName.Equals("area") || qName.Equals("base") || qName.Equals("basefont") || qName.Equals("br")
            || qName.Equals("col") || qName.Equals("frame") || qName.Equals("hr") || qName.Equals("img")
            || qName.Equals("input") || qName.Equals("isindex") || qName.Equals("link") || qName.Equals("meta")
            || qName.Equals("param")))) {
      Write("</");
      WriteName(uri, localName, qName, true);
      Write('>');
    }
    if (_elementLevel == 1) {
      Write('\n');
    }
    _cdataElement = false;
    base.EndElement(uri, localName, qName);
    _nsSupport.PopContext();
    _elementLevel--;
  }

  /// <summary>
  ///     Write character data.
  ///     Pass the event on down the filter chain for further processing.
  /// </summary>
  /// <param name="ch">
  ///     The array of characters to write.
  /// </param>
  /// <param name="start">
  ///     The starting position in the array.
  /// </param>
  /// <param name="length">
  ///     The number of characters to write.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the characters, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="IContentHandler.Characters" />
  public override void Characters(char[] ch, int start, int length) {
    if (!_cdataElement) {
      WriteEsc(ch, start, length, false);
    } else {
      for (int i = start; i < start + length; i++) {
        Write(ch[i]);
      }
    }
    base.Characters(ch, start, length);
  }

  /// <summary>
  ///     Write ignorable whitespace.
  ///     Pass the event on down the filter chain for further processing.
  /// </summary>
  /// <param name="ch">
  ///     The array of characters to write.
  /// </param>
  /// <param name="start">
  ///     The starting position in the array.
  /// </param>
  /// <param name="length">
  ///     The number of characters to write.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the whitespace, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="IContentHandler.IgnorableWhitespace" />
  public override void IgnorableWhitespace(char[] ch, int start, int length) {
    WriteEsc(ch, start, length, false);
    base.IgnorableWhitespace(ch, start, length);
  }

  /// <summary>
  ///     Write a processing instruction.
  ///     Pass the event on down the filter chain for further processing.
  /// </summary>
  /// <param name="target">
  ///     The PI target.
  /// </param>
  /// <param name="data">
  ///     The PI data.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the PI, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="IContentHandler.ProcessingInstruction" />
  public override void ProcessingInstruction(string target, string data) {
    Write("<?");
    Write(target);
    Write(' ');
    Write(data);
    Write("?>");
    if (_elementLevel < 1) {
      Write('\n');
    }
    base.ProcessingInstruction(target, data);
  }

  /// <summary>
  ///     Write an empty element.
  ///     This method writes an empty element tag rather than a start tag
  ///     followed by an end tag.  Both a <see cref="StartElement" />
  ///     and an <see cref="EndElement(string,string,string)" /> event will
  ///     be passed on down the filter chain.
  /// </summary>
  /// <param name="uri">
  ///     The element's Namespace URI, or the empty string
  ///     if the element has no Namespace or if Namespace
  ///     processing is not being performed.
  /// </param>
  /// <param name="localName">
  ///     The element's local name (without prefix).  This
  ///     parameter must be provided.
  /// </param>
  /// <param name="qName">
  ///     The element's qualified name (with prefix), or
  ///     the empty string if none is available.  This parameter
  ///     is strictly advisory: the writer may or may not use
  ///     the prefix attached.
  /// </param>
  /// <param name="atts">
  ///     The element's attribute list.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the empty tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="StartElement" />
  /// <seealso cref="EndElement(string,string,string) " />
  public virtual void EmptyElement(string uri, string localName, string qName, IAttributes atts) {
    _nsSupport.PushContext();
    Write('<');
    WriteName(uri, localName, qName, true);
    WriteAttributes(atts);
    if (_elementLevel == 1) {
      ForceNSDecls();
    }
    WriteNSDecls();
    Write("/>");
    base.StartElement(uri, localName, qName, atts);
    base.EndElement(uri, localName, qName);
  }

  /// <summary>
  ///     Start a new element without a qname or attributes.
  ///     <para>
  ///         This method will provide a default empty attribute
  ///         list and an empty string for the qualified name.
  ///         It invokes <see cref="StartElement(string, string, string, IAttributes)"/>
  ///         directly.
  ///     </para>
  /// </summary>
  /// <param name="uri">
  ///     The element's Namespace URI.
  /// </param>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the start tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="StartElement(string, string, string, IAttributes)" />
  public virtual void StartElement(string uri, string localName) {
    StartElement(uri, localName, "", _emptyAtts);
  }

  /// <summary>
  ///     Start a new element without a qname, attributes or a Namespace URI.
  ///     <para>
  ///         This method will provide an empty string for the
  ///         Namespace URI, and empty string for the qualified name,
  ///         and a default empty attribute list. It invokes
  ///         #startElement(string, string, string, Attributes)}
  ///         directly.
  ///     </para>
  /// </summary>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the start tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="StartElement(string, string, string, IAttributes)" />
  public void StartElement(string localName) {
    StartElement("", localName, "", _emptyAtts);
  }

  /// <summary>
  ///     End an element without a qname.
  ///     <para>
  ///         This method will supply an empty string for the qName.
  ///         It invokes <see cref="EndElement(string, string, string)" />
  ///         directly.
  ///     </para>
  /// </summary>
  /// <param name="uri">
  ///     The element's Namespace URI.
  /// </param>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the end tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="EndElement(string, string, string)" />
  public virtual void EndElement(string uri, string localName) {
    EndElement(uri, localName, "");
  }

  /// <summary>
  ///     End an element without a Namespace URI or qname.
  ///     <para>
  ///         This method will supply an empty string for the qName
  ///         and an empty string for the Namespace URI.
  ///         It invokes <see cref="EndElement(string, string, string)" />
  ///         directly.
  ///     </para>
  /// </summary>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the end tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="EndElement(string, string, string)" />
  public virtual void EndElement(string localName) {
    EndElement("", localName, "");
  }

  /// <summary>
  ///     Add an empty element without a qname or attributes.
  ///     <para>
  ///         This method will supply an empty string for the qname
  ///         and an empty attribute list.  It invokes
  ///         <see cref="EmptyElement(string, string, string, IAttributes)" />
  ///         directly.
  ///     </para>
  /// </summary>
  /// <param name="uri">
  ///     The element's Namespace URI.
  /// </param>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the empty tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="EmptyElement(string, string, string, IAttributes)" />
  public virtual void EmptyElement(string uri, string localName) {
    EmptyElement(uri, localName, "", _emptyAtts);
  }

  /// <summary>
  ///     Add an empty element without a Namespace URI, qname or attributes.
  ///     <para>
  ///         This method will supply an empty string for the qname,
  ///         and empty string for the Namespace URI, and an empty
  ///         attribute list.  It invokes
  ///         <see cref="EmptyElement(string, string, string, IAttributes)" />
  ///         directly.
  ///     </para>
  /// </summary>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the empty tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="EmptyElement(string, string, string, IAttributes)" />
  public virtual void EmptyElement(string localName) {
    EmptyElement("", localName, "", _emptyAtts);
  }

  /// <summary>
  ///     Write an element with character data content.
  ///     <para>
  ///         This is a convenience method to write a complete element
  ///         with character data content, including the start tag
  ///         and end tag.
  ///     </para>
  ///     <para>
  ///         This method invokes
  ///         <see cref="StartElement(string, string, string, IAttributes)" />,
  ///         followed by
  ///         <see cref="Characters(string)" />, followed by
  ///         <see cref="EndElement(string, string, string)" />.
  ///     </para>
  /// </summary>
  /// <param name="uri">
  ///     The element's Namespace URI.
  /// </param>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <param name="qName">
  ///     The element's default qualified name.
  /// </param>
  /// <param name="atts">
  ///     The element's attributes.
  /// </param>
  /// <param name="content">
  ///     The character data content.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the empty tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="StartElement(string, string, string, IAttributes)" />
  /// <seealso cref="Characters(string)" />
  /// <seealso cref="EndElement(string, string, string)" />
  public virtual void DataElement(string uri, string localName, string qName, IAttributes atts, string content) {
    StartElement(uri, localName, qName, atts);
    Characters(content);
    EndElement(uri, localName, qName);
  }

  /// <summary>
  ///     Write an element with character data content but no attributes.
  ///     <para>
  ///         This is a convenience method to write a complete element
  ///         with character data content, including the start tag
  ///         and end tag.  This method provides an empty string
  ///         for the qname and an empty attribute list.
  ///     </para>
  ///     <para>
  ///         This method invokes
  ///         <see cref="StartElement(string, string, string, IAttributes)" />,
  ///         followed by
  ///         <see cref="Characters(string)" />, followed by
  ///         <see cref="EndElement(string, string, string)" />.
  ///     </para>
  /// </summary>
  /// <param name="uri">
  ///     The element's Namespace URI.
  /// </param>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <param name="content">
  ///     The character data content.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the empty tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="StartElement(string, string, string, IAttributes)" />
  /// <seealso cref="Characters(string)" />
  /// <seealso cref="EndElement(string, string, string)" />
  public virtual void DataElement(string uri, string localName, string content) {
    DataElement(uri, localName, "", _emptyAtts, content);
  }

  /// <summary>
  ///     Write an element with character data content but no attributes or Namespace URI.
  ///     <para>
  ///         This is a convenience method to write a complete element
  ///         with character data content, including the start tag
  ///         and end tag.  The method provides an empty string for the
  ///         Namespace URI, and empty string for the qualified name,
  ///         and an empty attribute list.
  ///     </para>
  ///     <para>
  ///         This method invokes
  ///         <see cref="StartElement(string, string, string, IAttributes)" />,
  ///         followed by
  ///         <see cref="Characters(string)" />, followed by
  ///         <see cref="EndElement(string, string, string)" />.
  ///     </para>
  /// </summary>
  /// <param name="localName">
  ///     The element's local name.
  /// </param>
  /// <param name="content">
  ///     The character data content.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the empty tag, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="StartElement(string, string, string, IAttributes)" />
  /// <seealso cref="Characters(string)" />
  /// <seealso cref="EndElement(string, string, string)" />
  public virtual void DataElement(string localName, string content) {
    DataElement("", localName, "", _emptyAtts, content);
  }

  /// <summary>
  ///     Write a string of character data, with XML escaping.
  ///     <para>
  ///         This is a convenience method that takes an XML
  ///         string, converts it to a character array, then invokes
  ///         <see cref="Characters(char[], int, int)" />.
  ///     </para>
  /// </summary>
  /// <param name="data">
  ///     The character data.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error
  ///     writing the string, or if a handler further down
  ///     the filter chain raises an exception.
  /// </exception>
  /// <seealso cref="Characters(char[], int, int)" />
  public virtual void Characters(string data) {
    char[] ch = data.ToCharArray();
    Characters(ch, 0, ch.Length);
  }

  /// <summary>
  ///     Force all Namespaces to be declared.
  ///     This method is used on the root element to ensure that
  ///     the predeclared Namespaces all appear.
  /// </summary>
  private void ForceNSDecls() {
    foreach (string prefix in _forcedDeclTable.Keys) {
      DoPrefix(prefix, null, true);
    }
  }

  /// <summary>
  ///     Determine the prefix for an element or attribute name.
  ///     TODO: this method probably needs some cleanup.
  /// </summary>
  /// <param name="uri">
  ///     The Namespace URI.
  /// </param>
  /// <param name="qName">
  ///     The qualified name (optional); this will be used
  ///     to indicate the preferred prefix if none is currently
  ///     bound.
  /// </param>
  /// <param name="isElement">
  ///     true if this is an element name, false
  ///     if it is an attribute name (which cannot use the
  ///     default Namespace).
  /// </param>
  private string DoPrefix(string uri, string qName, bool isElement) {
    string defaultNS = _nsSupport.GetUri("");
    if ("".Equals(uri)) {
      if (isElement && defaultNS != null) {
        _nsSupport.DeclarePrefix("", "");
      }
      return null;
    }
    string prefix;
    if (isElement && defaultNS != null && uri.Equals(defaultNS)) {
      prefix = "";
    } else {
      prefix = _nsSupport.GetPrefix(uri);
    }
    if (prefix != null) {
      return prefix;
    }
    bool containsPrefix = _doneDeclTable.ContainsKey(uri);
    prefix = (string)(containsPrefix ? _doneDeclTable[uri] : null);
    if (containsPrefix && ((!isElement || defaultNS != null) && "".Equals(prefix) || _nsSupport.GetUri(prefix) != null)) {
      prefix = null;
    }
    if (prefix == null) {
      containsPrefix = _prefixTable.ContainsKey(uri);
      prefix = (string)(containsPrefix ? _prefixTable[uri] : null);
      if (containsPrefix
          && ((!isElement || defaultNS != null) && "".Equals(prefix) || _nsSupport.GetUri(prefix) != null)) {
        prefix = null;
      }
    }
    if (prefix == null && qName != null && !"".Equals(qName)) {
      int i = qName.IndexOf(':');
      if (i == -1) {
        if (isElement && defaultNS == null) {
          prefix = "";
        }
      } else {
        prefix = qName.Substring(0, i);
      }
    }
    for (; prefix == null || _nsSupport.GetUri(prefix) != null; prefix = "__NS" + ++_prefixCounter) {
    }
    _nsSupport.DeclarePrefix(prefix, uri);
    _doneDeclTable[uri] = prefix;
    return prefix;
  }

  /// <summary>
  ///     Write a raw character.
  /// </summary>
  /// <param name="c">
  ///     The character to write.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error writing
  ///     the character, this method will throw an IOException
  ///     wrapped in a SAXException.
  /// </exception>
  private void Write(char c) {
    try {
      _output.Write(c);
    } catch (IOException e) {
      throw new SAXException(e.Message, e);
    }
  }

  /// <summary>
  ///     Write a raw string.
  /// </summary>
  /// <param name="s"></param>
  /// <exception cref="SAXException">
  ///     If there is an error writing the string,
  ///     this method will throw an IOException wrapped in a SAXException
  /// </exception>
  private void Write(string s) {
    try {
      _output.Write(s);
    } catch (IOException e) {
      throw new SAXException(e.Message, e);
    }
  }

  /// <summary>
  ///     Write out an attribute list, escaping values.
  ///     The names will have prefixes added to them.
  /// </summary>
  /// <param name="atts">
  ///     The attribute list to write.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error writing
  ///     the attribute list, this method will throw an
  ///     IOException wrapped in a SAXException.
  /// </exception>
  private void WriteAttributes(IAttributes atts) {
    int len = atts.Length;
    for (int i = 0; i < len; i++) {
      char[] ch = atts.GetValue(i).ToCharArray();
      Write(' ');
      WriteName(atts.GetUri(i), atts.GetLocalName(i), atts.GetQName(i), false);
      if (_htmlMode && BoolAttribute(atts.GetLocalName(i), atts.GetQName(i), atts.GetValue(i))) {
        break;
      }
      Write("=\"");
      WriteEsc(ch, 0, ch.Length, true);
      Write('"');
    }
  }

  // Return true if the attribute is an HTML bool from the above list.
  private bool BoolAttribute(string localName, string qName, string value) {
    string name = localName;
    if (name == null) {
      int i = qName.IndexOf(':');
      if (i != -1) {
        name = qName.Substring(i + 1, qName.Length);
      }
    }
    if (!name.Equals(value)) {
      return false;
    }
    for (int j = 0; j < _bools.Length; j++) {
      if (name.Equals(_bools[j])) {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  ///     Write an array of data characters with escaping.
  /// </summary>
  /// <param name="ch">
  ///     The array of characters.
  /// </param>
  /// <param name="start">
  ///     The starting position.
  /// </param>
  /// <param name="length">
  ///     The number of characters to use.
  /// </param>
  /// <param name="isAttVal">
  ///     true if this is an attribute value literal.
  /// </param>
  /// <exception cref="SAXException">
  ///     If there is an error writing
  ///     the characters, this method will throw an
  ///     IOException wrapped in a SAXException.
  /// </exception>
  private void WriteEsc(char[] ch, int start, int length, bool isAttVal) {
    for (int i = start; i < start + length; i++) {
      switch (ch[i]) {
        case '&':
          Write("&amp;");
          break;
        case '<':
          Write("&lt;");
          break;
        case '>':
          Write("&gt;");
          break;
        case '\"':
          if (isAttVal) {
            Write("&quot;");
          } else {
            Write('\"');
          }
          break;
        default:
          if (!_unicodeMode && ch[i] > '\u007f') {
            Write("&#");
            Write(((int)ch[i]).ToString(CultureInfo.InvariantCulture));
            Write(';');
          } else {
            Write(ch[i]);
          }
          break;
      }
    }
  }

  /// <summary>
  ///     Write out the list of Namespace declarations.
  /// </summary>
  /// <exception cref="SAXException">
  ///     This method will throw
  ///     an IOException wrapped in a SAXException if
  ///     there is an error writing the Namespace
  ///     declarations.
  /// </exception>
  private void WriteNSDecls() {
    IEnumerable prefixes = _nsSupport.GetDeclaredPrefixes();
    foreach (string prefix in prefixes) {
      string uri = _nsSupport.GetUri(prefix);
      if (uri == null) {
        uri = "";
      }
      char[] ch = uri.ToCharArray();
      Write(' ');
      if ("".Equals(prefix)) {
        Write("xmlns=\"");
      } else {
        Write("xmlns:");
        Write(prefix);
        Write("=\"");
      }
      WriteEsc(ch, 0, ch.Length, true);
      Write('\"');
    }
  }

  /// <summary>
  ///     Write an element or attribute name.
  /// </summary>
  /// <param name="uri">
  ///     The Namespace URI.
  /// </param>
  /// <param name="localName">
  ///     The local name.
  /// </param>
  /// <param name="qName">
  ///     The prefixed name, if available, or the empty string.
  /// </param>
  /// <param name="isElement">
  ///     true if this is an element name, false if it
  ///     is an attribute name.
  /// </param>
  /// <exception cref="SAXException">
  ///     This method will throw an
  ///     IOException wrapped in a SAXException if there is
  ///     an error writing the name.
  /// </exception>
  private void WriteName(string uri, string localName, string qName, bool isElement) {
    string prefix = DoPrefix(uri, qName, isElement);
    if (prefix != null && !"".Equals(prefix)) {
      Write(prefix);
      Write(':');
    }
    if (localName != null && !"".Equals(localName)) {
      Write(localName);
    } else {
      int i = qName.IndexOf(':');
      Write(qName.Substring(i + 1, qName.Length - (i + 1)));
    }
  }

  public string GetOutputProperty(string key) {
    return _outputProperties[key];
  }

  public void SetOutputProperty(string key, string value) {
    _outputProperties[key] = value;
    //	System.out.println("%%%% key = [" + key + "] value = [" + value +"]");
    if (key.Equals(ENCODING)) {
      _outputEncoding = value;
      _unicodeMode = value.Substring(0, 3).Equals("utf", StringComparison.OrdinalIgnoreCase);
      //                System.out.println("%%%% unicodeMode = " + unicodeMode);
    } else if (key.Equals(METHOD)) {
      _htmlMode = value.Equals("html");
    } else if (key.Equals(DOCTYPE_PUBLIC)) {
      _overridePublic = value;
      _forceDtd = true;
    } else if (key.Equals(DOCTYPE_SYSTEM)) {
      _overrideSystem = value;
      _forceDtd = true;
    } else if (key.Equals(VERSION)) {
      _version = value;
    } else if (key.Equals(STANDALONE)) {
      _standalone = value;
    }
    //	System.out.println("%%%% htmlMode = " + htmlMode);
  }
}
