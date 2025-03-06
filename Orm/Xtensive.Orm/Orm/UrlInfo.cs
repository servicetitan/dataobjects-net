// Copyright (C) 2007-2021 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alex Yakunin
// Created:    2007.06.08

using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Xtensive.Core;
using Xtensive.Comparison;

namespace Xtensive.Orm;

/// <summary>
/// Holds an URL and provides easy access to its different parts.
/// </summary>
/// <remarks>
/// <para>
/// The common URL format that would be converted
/// to the <see cref="UrlInfo"/> can be represented
/// in the BNF form as following:
/// <code lang="BNF" outline="true">
/// url ::= protocol://[user[:password]@]host[:port]/resource[?parameters]
/// protocol ::= alphanumx[protocol]
/// user ::= alphanumx[user]
/// password ::= alphanumx[password]
/// host ::= hostname | hostnum
/// port ::= digits
/// resource ::= name
/// parameters ::= parameter[&amp;parameter]
///
/// hostname ::= name[.hostname]
/// hostnum ::= digits.digits.digits.digits
///
/// parameter ::= name=[name]
///
/// name ::= alpanumx[name]
///
/// digits ::= digit[digits]
/// alphanumx ::= alphanum | escape | $ | - | _ | . | + | ! | * | " | ' | ( | ) | , | ; | # | space
/// alphanum ::= alpha | digit
/// escape ::= % hex hex
/// hex ::= digit | a | b | c | d | e | f | A | B | C | D | E | F
/// digit ::= 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9
/// alpha ::= /* represents any unicode alpa character */
/// </code>
/// </para>
/// <note>
/// This not fully precise notation because it slightly simplified to be shorter.
/// But it almost completely reflects <see cref="UrlInfo"/> URL parser
/// capabilities.
/// </note>
/// <para>
/// Here you can see several valid URL samples:
/// <pre>
/// tcp://localhost/
/// tcp://server:40000/myResource
/// tcp://admin:admin@localhost:40000/myResource?askTimeout=60
/// </pre>
/// </para>
/// </remarks>
[Serializable]
[DebuggerDisplay("{Url}")]
[TypeConverter(typeof(UrlInfoConverter))]
public sealed record UrlInfo
(
) : IComparable<UrlInfo>
{
  private static readonly Regex Pattern = new Regex(
        @"^(?'proto'[^:]*[^sS])(?'secure'[sS]?)://" +
        @"((?'username'[^:@]*)" +
        @"(:(?'password'[^@]*))?@)?" +
        @"(?'host'[^:/]*)" +
        @"(:(?'port'\d+))?" +
        @"/(?'resource'[^?]*)?" +
        @"(\?(?'params'.*))?",
        RegexOptions.Compiled|RegexOptions.Singleline);

  /// <summary>
  /// Gets an URL this instance describes.
  /// </summary>
  public string Url
  {
    get {
      if (field is null) {
        StringBuilder sb = new(100);
        sb.Append($"{Protocol}{(Secure ? "s" : "")}://");
        if (!string.IsNullOrEmpty(User)) {
          sb.Append(UrlEncode(User));
          if (!string.IsNullOrEmpty(Password)) {
            sb.Append($":{UrlEncode(Password)}");
          }
          sb.Append('@');
        }

        sb.Append(UrlEncode(Host));
        if (Port != 0) {
          sb.Append($":{Port}");
        }

        if (!string.IsNullOrEmpty(Resource)) {
          sb.Append($"/{Resource}");
        }

        if (Params.Count > 0) {
          sb.Append('?');
          sb.Append(string.Join("&", Params.Select(kv =>$"{UrlEncode(kv.Key)}={UrlEncode(kv.Value)}")));
        }
        field = sb.ToString();
      }
      return field;
    }
    private set;
  }

  /// <summary>
  /// Gets the protocol part of the current <see cref="Url"/>
  /// (e.g. <b>"tcp"</b> is the protocol part of the "<b>tcp</b>://admin:password@localhost/resource" URL).
  /// </summary>
  public string Protocol
  {
    [DebuggerStepThrough] get => field;
    set {
      field = value;
      Url = null;
    }
  } = string.Empty;

  /// <summary>
  /// Gets the security part of the current <see cref="Url"/>
  /// Scheme with 's' suffix is secure.
  /// </summary>
  public bool Secure
  {
    [DebuggerStepThrough] get => field;
    set {
      field = value;
      Url = null;
    }
  }

  /// <summary>
  /// Gets the host part of the current <see cref="Url"/>
  /// (e.g. <b>"localhost"</b> is the host part of the "tcp://admin:password@<b>localhost</b>/resource" URL).
  /// </summary>
  public string Host
  {
    [DebuggerStepThrough] get;
    set {
      field = value;
      Url = null;
    }
  } = string.Empty;

  /// <summary>
  /// Gets the port part of the current <see cref="Url"/>
  /// (e.g. <b>40000</b> is the port part of the "tcp://admin:password@localhost:<b>40000</b>/resource" URL).
  /// </summary>
  public int Port
  {
    [DebuggerStepThrough] get;
    set {
      field = value;
      Url = null;
    }
  }

  /// <summary>
  /// Gets the resource name part of the current <see cref="Url"/>
  /// (e.g. <b>"resource"</b> is the resource name part of the "tcp://admin:password@localhost/<b>resource</b>" URL).
  /// </summary>
  public string Resource
  {
    [DebuggerStepThrough] get;
    set {
      field = value;
      Url = null;
    }
  } = string.Empty;

  /// <summary>
  /// Gets the user name part of the current <see cref="Url"/>
  /// (e.g. <b>"admin"</b> is the user name part of the "tcp://<b>admin</b>:password@localhost/resource" URL).
  /// </summary>
  public string User
  {
    [DebuggerStepThrough] get => field;
    set {
      field = value;
      Url = null;
    }
  } = string.Empty;

  /// <summary>
  /// Gets the password part of the current <see cref="Url"/>
  /// (e.g. <b>"password"</b> is the password part of the "tcp://admin:<b>password</b>@localhost/resource" URL).
  /// </summary>
  public string Password
  {
    [DebuggerStepThrough] get => field;
    set {
      field = value;
      Url = null;
    }
  } = string.Empty;

  /// <summary>
  /// Gets additional parameters of the current <see cref="Url"/>
  /// (e.g. <b>"param1=value1&amp;param2=value2"</b> is the additional parameters part
  /// of the "tcp://admin:password@localhost/resource?<b>param1=value1&amp;param2=value2</b>" URL).
  /// </summary>
  /// <remarks>
  /// <para>The mentioned part of the <see cref="Url"/> is parsed
  /// and represented in a <see cref="Dictionary{String,String}"/> form.</para>
  /// </remarks>
  public IReadOnlyDictionary<string, string> Params
  {
    [DebuggerStepThrough]
    get => field;
    set {
      field = value;
      Url = null;
    }
  }

  /// <summary>
  /// Splits URL into parts (protocol, host, port, resource, user, password) and set all
  /// derived values to the corresponding properties of the instance.
  /// </summary>
  /// <param name="url">URL to split</param>
  /// <remarks>
  /// The expected URL format is as the following:
  /// proto://[[user[:password]@]host[:port]]/resource.
  /// Note that the empty URL will cause an exception.
  /// </remarks>
  /// <exception cref="ArgumentException">Specified <paramref name="url"/> is invalid (cannot be parsed).</exception>
  public static UrlInfo Parse(string url)
  {
    var result = new UrlInfo();
    Parse(url, result);
    return result;
  }

  private static void Parse(string url, UrlInfo info)
  {
    try {
      string tUrl = url;
      if (tUrl.Length==0)
        tUrl = ":///";

      var result = Pattern.Match(tUrl);
      if (!result.Success)
        throw Exceptions.InvalidUrl(url, "url");

      int @port = 0;

      if (result.Result("${port}").Length!=0)
        @port = int.Parse(result.Result("${port}"));
      if (@port<0 || @port>65535)
        throw Exceptions.InvalidUrl(url, "port");

      string tParams = result.Result("${params}");
      string[] aParams = tParams.Split('&');
      var parameters = new SortedDictionary<string, string>();
      if (tParams!=string.Empty) {
        foreach (string sPair in aParams) {
          string[] aNameValue = sPair.Split(new char[] {'='}, 2);
          if (aNameValue.Length!=2)
            throw Exceptions.InvalidUrl(url, "parameters");
          parameters.Add(UrlDecode(aNameValue[0]), UrlDecode(aNameValue[1]));
        }
      }

      info.User = UrlDecode(result.Result("${username}"));
      info.Password = UrlDecode(result.Result("${password}"));
      info.Resource = UrlDecode(result.Result("${resource}"));
      info.Host = UrlDecode(result.Result("${host}"));
      info.Protocol = UrlDecode(result.Result("${proto}"));
      info.Secure = !string.IsNullOrEmpty(result.Result("${secure}"));
      info.Port = @port;
      info.Params = parameters;
    }
    catch (Exception e) when (!(e is ArgumentException or InvalidOperationException)) {
      throw Exceptions.InvalidUrl(url, "url");
    }
  }

  private class UrlDecoder
  {
    // Fields
    private int m_bufferSize;
    private byte[] m_byteBuffer;
    private char[] m_charBuffer;
    private Encoding m_encoding;
    private int m_numBytes;
    private int m_numChars;

    // Methods
    internal UrlDecoder(int bufferSize, Encoding encoding)
    {
      m_bufferSize = bufferSize;
      m_encoding = encoding;
      m_charBuffer = new char[bufferSize];
    }

    internal void AddByte(byte b)
    {
      if (m_byteBuffer==null)
        m_byteBuffer = new byte[m_bufferSize];
      m_byteBuffer[m_numBytes++] = b;
    }

    internal void AddChar(char ch)
    {
      if (m_numBytes>0)
        FlushBytes();
      m_charBuffer[m_numChars++] = ch;
    }

    private void FlushBytes()
    {
      if (m_numBytes>0) {
        m_numChars += m_encoding.GetChars(m_byteBuffer, 0, m_numBytes, m_charBuffer, m_numChars);
        m_numBytes = 0;
      }
    }

    internal string GetString()
    {
      if (m_numBytes>0)
        FlushBytes();
      if (m_numChars>0)
        return new string(m_charBuffer, 0, m_numChars);
      return string.Empty;
    }
  }

  private static string UrlEncode(string str) => Uri.EscapeDataString(str);

  private static string UrlDecode(string s, Encoding e = null)
  {
    e ??= Encoding.UTF8;
    int len = s.Length;
    UrlDecoder decoder = new UrlDecoder(len, e);
    for (int i = 0; i<len; i++) {
      char c = s[i];
      if (c=='+') {
        c = ' ';
      }
      else if (c=='%' && i<(len-2)) {
        if (s[i+1]=='u' && i<(len-5)) {
          int num3 = HexToInt(s[i+2]);
          int num4 = HexToInt(s[i+3]);
          int num5 = HexToInt(s[i+4]);
          int num6 = HexToInt(s[i+5]);
          if ((num3<0 || num4<0) || (num5<0 || num6<0))
            goto loc_1;
          c = (char)((ushort)((((num3 << 12)|(num4 << 8))|(num5 << 4))|num6));
          i += 5;
          decoder.AddChar(c);
          continue;
        }
        int num7 = HexToInt(s[i+1]);
        int num8 = HexToInt(s[i+2]);
        if (num7>=0 && num8>=0) {
          byte num9 = (byte)((num7 << 4)|num8);
          i += 2;
          decoder.AddByte(num9);
          continue;
        }
      }
      loc_1:
      if ((c&0xff80)=='\0')
        decoder.AddByte((byte)c);
      else
        decoder.AddChar(c);
    }
    return decoder.GetString().Trim();
  }

  private static int HexToInt(char h) =>
    h is >= '0' and <= '9' ? h - '0'
    : h is >= 'a' and <= 'f' ? h - 'a' + 10
    : h is >= 'A' and <= 'F' ? h - 'A' + 10
    : -1;

  /// <inheritdoc/>
  public bool Equals(UrlInfo other) =>
    AdvancedComparerStruct<string>.System.Equals(Url, other.Url);

  /// <inheritdoc/>
  public int CompareTo(UrlInfo other) =>
    AdvancedComparerStruct<string>.System.Compare(Url, other.Url);

  /// <inheritdoc/>
  public override int GetHashCode() => Url.GetHashCode();

  /// <inheritdoc/>
  public override string ToString() => Url;
}
