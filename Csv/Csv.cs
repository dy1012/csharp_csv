using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Csv {
  /// <summary>
  /// CSV関連の処理をまとめたクラス。
  /// リフレクション使いまくってるので遅いと思われる。
  /// </summary>
  /// <typeparam name="T"></typeparam>
  public class Csv<T> : IEnumerable<T> where T : class, new() {
    /// <summary>
    /// 既定の区切り文字列。
    /// </summary>
    const string DEFAULT_DELIMITER = ",";

    /// <summary>
    /// 行の抽出を行う際に使用する正規表現。
    /// </summary>
    readonly Regex regexLine = new Regex("^.*(?:\\n|$)", System.Text.RegularExpressions.RegexOptions.Multiline);

    /// <summary>
    /// フィールドの抽出を行う際に使用する正規表現。
    /// </summary>
    readonly Regex regexField = new Regex("\\s*(\"(?:[^\"]|\"\")*\"|[^,]*)\\s*,", System.Text.RegularExpressions.RegexOptions.None);

    /// <summary>
    /// カラム情報を格納するリスト。
    /// </summary>
    IList<CsvColumn<T>> _columns = new List<CsvColumn<T>>();

    /// <summary>
    /// 区切り文字を取得、設定。
    /// </summary>
    public string Delimiter { get; set; }

    /// <summary>
    /// 見出しを含むCSVかどうかを表すbool値を取得、設定。
    /// </summary>
    public bool IsContainHeader { get; set; }

    /// <summary>
    /// CSVファイルにエクスポート。
    /// </summary>
    /// <param name="source">エクスポートを行うオブジェクトのリスト</param>
    /// <param name="filePath">ファイルパス</param>
    /// <param name="encoding">ファイルのエンコード(既定はUTF-8)</param>
    public void Export(IEnumerable<T> source, string filePath, Encoding encoding = null) {
      var result = new List<string>();
      var delimiter = string.IsNullOrEmpty(this.Delimiter) ? DEFAULT_DELIMITER : this.Delimiter;

      if (this.IsContainHeader)
        result.Add(this.CreateHeader(delimiter));

      foreach (var item in source) {
        var values = new List<string>();

        foreach (var column in this._columns) {
          if (column.IsConstantValue) {
            values.Add(column.ConstantValue);
          }
          else {
            var propertyChain = ExpressionUtil.GetPropertyChain(column.Property);
            object value = item;
            foreach (var chainItem in propertyChain.Reverse())
              value = chainItem.Value.GetValue(value);

            if (column.Converter != null)
              values.Add(column.Converter.ObjectToCsv(value));
            else
              values.Add(value != null ? value.ToString() : string.Empty);
          }
        }

        result.Add(string.Join(delimiter, values));
      }

      encoding = encoding ?? Encoding.UTF8;
      File.WriteAllLines(filePath, result, encoding);
    }

    /// <summary>
    /// CSVファイルからインポート。
    /// </summary>
    /// <param name="filePath">CSVファイルのパス</param>
    /// <param name="encoding">ファイルのエンコード(既定はUTF-8)</param>
    /// <returns></returns>
    public IEnumerable<T> Import(string filePath, Encoding encoding = null) {
      var file = new FileInfo(filePath);
      if (!file.Exists)
        throw new ArgumentException("CSVファイルが見つかりません。");

      using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) {
        return this.Import(stream, encoding);
      }
    }

    /// <summary>
    /// CSVファイルからインポート。
    /// </summary>
    /// <param name="stream">CSVファイルのファイルストリーム</param>
    /// <param name="encoding">ファイルのエンコード(既定はUTF-8)</param>
    /// <returns></returns>
    public IEnumerable<T> Import(FileStream stream, Encoding encoding = null) {
      encoding = encoding ?? Encoding.UTF8;
      using (var reader = new StreamReader(stream, encoding)) {
        return this.Parse(reader.ReadToEnd());
      }
    }

    /// <summary>
    /// CSV形式の文字列からオブジェクトのリストを作成。
    /// </summary>
    /// <param name="csv"></param>
    /// <returns></returns>
    public IEnumerable<T> Parse(string csv) {
      var delimiter = string.IsNullOrEmpty(this.Delimiter) ? DEFAULT_DELIMITER : this.Delimiter;
      var lines = new List<IEnumerable<string>>();
      IEnumerable<string> headers = null;
      csv = csv.Trim(new[] { '\r', '\n' });
      for (var mLine = regexLine.Match(csv); mLine.Success; mLine = mLine.NextMatch()) {
        var line = mLine.Value;
        while (this.HasGroupText(line)) {
          mLine = mLine.NextMatch();
          if (!mLine.Success)
            throw new ApplicationException("CSVファイルのフォーマットが不正です。");

          line += mLine.Value;
        }

        line = string.Format("{0}{1}", line.TrimEnd(new[] { '\r', '\n' }), delimiter);
        if (this.IsContainHeader && headers == null)
          headers = this.GetFields(line);
        else
          lines.Add(this.GetFields(line));
      }

      return this.CreateObjects(headers, lines);
    }

    /// <summary>
    /// 値にダブルクォーテーションが含まれており、改行も含まれている場合trueを返却。
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    bool HasGroupText(string source) {
      int found = 0;
      for (var i = source.IndexOf('"'); i > -1; i = source.IndexOf('"', i + 1))
        found++;

      return (found % 2) == 1;
    }

    /// <summary>
    /// 値のリストを取得。
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>
    IEnumerable<string> GetFields(string source) {
      var result = new List<string>();
      for (var mField = regexField.Match(source); mField.Success; mField = mField.NextMatch()) {
        var field = mField.Groups[1].Value.Trim();
        if (field.StartsWith("\"") && field.EndsWith("\""))
          field = field.Substring(1, field.Length - 2).Replace("\"\"", "\"");

        result.Add(field);
      }

      return result;
    }

    /// <summary>
    /// 設定されているカラム情報を使って、結果オブジェクトを作成。
    /// </summary>
    /// <param name="headers"></param>
    /// <param name="lines"></param>
    /// <returns></returns>
    IEnumerable<T> CreateObjects(IEnumerable<string> headers, IList<IEnumerable<string>> lines) {
      headers = headers ?? this.CreateHeaders();
      var result = new List<T>();
      foreach (var values in lines) {
        var item = new T();
        for (var i = 0; i < headers.Count(); i++) {
          var header = headers.ElementAt(i);
          var column = this._columns.Where(x => x.ColumnName == header).SingleOrDefault();
          if (column == null)
            continue;

          var value = column.IsConstantValue ? column.ConstantValue : values.ElementAt(i);
          if (string.IsNullOrEmpty(value))
            continue;

          object current = item;
          var propertyChain = ExpressionUtil.GetPropertyChain(column.Property).Reverse();
          for (var j = 0; j < propertyChain.Count() - 1; j++) {
            var chainItem = propertyChain.ElementAt(j);
            var cValue = chainItem.Value.GetValue(current) ?? Activator.CreateInstance(chainItem.Value.PropertyType);
            chainItem.Value.SetValue(current, cValue);
            current = cValue;
          }

          var lastItem = propertyChain.Last();
          if (column.Converter != null)
            lastItem.Value.SetValue(current, column.Converter.CsvToObject(value));
          else
            lastItem.Value.SetValue(current, lastItem.Value.Converter.ConvertFrom(value));
        }

        result.Add(item);
      }

      return result;
    }

    /// <summary>
    /// 出力用のヘッダを作成。
    /// </summary>
    /// <param name="delimiter"></param>
    /// <returns></returns>
    string CreateHeader(string delimiter) {
      return string.Join(delimiter, this.CreateHeaders());
    }

    /// <summary>
    /// カラム情報からヘッダのリストを作成。
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> CreateHeaders() {
      return this._columns.Select(x => x.ColumnName);
    }

    /// <summary>
    /// カラム情報を追加。
    /// </summary>
    /// <param name="columnName">カラム(ヘッダ)名</param>
    /// <param name="propertyExpression">このカラムと紐付けを行うプロパティ</param>
    public void Add(string columnName, Expression<Func<T, object>> propertyExpression) {
      this.Add(columnName, propertyExpression, null);
    }

    /// <summary>
    /// カラム情報を追加。
    /// </summary>
    /// <param name="columnName">カラム(ヘッダ)名</param>
    /// <param name="propertyExpression">このカラムと紐付けを行うプロパティ</param>
    /// <param name="converter">値の変換を行うオブジェクト</param>
    public void Add(string columnName, Expression<Func<T, object>> propertyExpression, ICsvConverter<T> converter) {
      this.Add(columnName, propertyExpression, converter, null, false);
    }

    /// <summary>
    /// 固定値カラム情報を追加。
    /// </summary>
    /// <param name="columnName">カラム(ヘッダ)名</param>
    /// <param name="constantValue">固定値</param>
    /// <param name="propertyExpression">このカラムと紐付けを行うプロパティ</param>
    public void Add(string columnName, string constantValue, Expression<Func<T, object>> propertyExpression) {
      this.Add(columnName, propertyExpression, null, constantValue, true);
    }

    /// <summary>
    /// カラム情報を追加。
    /// </summary>
    /// <param name="columnName"></param>
    /// <param name="propertyExpression"></param>
    /// <param name="converter"></param>
    /// <param name="constantValue"></param>
    /// <param name="isContantValue"></param>
    void Add(string columnName, Expression<Func<T, object>> propertyExpression, ICsvConverter<T> converter, string constantValue, bool isContantValue) {
      this._columns.Add(new CsvColumn<T>() {
        ColumnName = columnName,
        Property = propertyExpression,
        Converter = converter,
        IsConstantValue = isContantValue,
        ConstantValue = constantValue
      });
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
      throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      throw new NotImplementedException();
    }
  }
}
