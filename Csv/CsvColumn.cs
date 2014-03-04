using System;
using System.Linq.Expressions;

namespace Csv {
  public class CsvColumn<T> {
    /// <summary>
    /// カラム名を取得、設定。
    /// </summary>
    public string ColumnName { get; set; }

    /// <summary>
    /// カラムと紐付けを行うプロパティのラムダ式を取得、設定。
    /// </summary>
    public Expression<Func<T, object>> Property { get; set; }

    /// <summary>
    /// 値の変換を行うオブジェクトを取得、設定。
    /// </summary>
    public ICsvConverter<T> Converter { get; set; }

    /// <summary>
    /// 固定値かどうかのフラグを取得、設定。
    /// </summary>
    public bool IsConstantValue { get; set; }

    /// <summary>
    /// 固定値を取得、設定。
    /// </summary>
    public string ConstantValue { get; set; }
  }
}
