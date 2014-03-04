namespace Csv {
  public interface ICsvConverter<T> {
    /// <summary>
    /// オブジェクトからCSVファイルへの値変換。
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    string ObjectToCsv(object value);

    /// <summary>
    /// CSVファイルからオブジェクトへの値変換。
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    object CsvToObject(string value);
  }
}
