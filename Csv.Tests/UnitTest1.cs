using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace Csv.Tests {
  [TestClass]
  public class UnitTest1 {
    [TestMethod]
    public void TestMethod1() {
      var csv = new Csv<TestModel>() {
        { "ひとつ", x => x.Text1 },
        { "ふたつ", x => x.Date1, new TestConverter() },
        { "みっつ", x => x.Composite1.Text1 },
        { "よっつ", x => x.Composite1.Nest1.Text1 },
        { "いつつ", "こていちだよ", x => x.Composite1.Constant1 }
      };

      var objs = new List<TestModel>() {
        { this.GetModel(1) },
        { this.GetModel(2) },
        { this.GetModelAllNull() }
      };

      var path = @"test.csv";

      csv.IsContainHeader = true;
      csv.Export(objs, path);

      var result = csv.Import(path);

      Assert.AreEqual(result.Count(), objs.Count);
      Assert.AreEqual(result.ElementAt(0), objs.ElementAt(0));
      Assert.AreEqual(result.ElementAt(1), objs.ElementAt(1));
      Assert.AreEqual(result.ElementAt(2), objs.ElementAt(2));
      File.Delete(path);
    }

    TestModel GetModel(int index) {
      return new TestModel() {
        Text1 = string.Format("ひとつめ{0}", index),
        Date1 = DateTime.Now.Date.AddMonths(index - 1),
        Composite1 = new CompositeModel() {
          Text1 = string.Format("とじこめちゃったひとつめ{0}", index),
          Constant1 = "こていちだよ",
          Nest1 = new NestCompositeModel() {
            Text1 = string.Format("ねすとしちゃったひとつめ{0}", index)
          }
        }
      };
    }

    TestModel GetModelAllNull() {
      return new TestModel() {
        Composite1 = new CompositeModel() {
          Constant1 = "こていちだよ"
        }
      };
    }
  }

  public class TestModel {
    public string Text1 { get; set; }
    public DateTime Date1 { get; set; }
    public CompositeModel Composite1 { get; set; }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj == null)
        return false;

      var source = (TestModel)obj;
      return this.Text1 == source.Text1 && this.Date1 == source.Date1
        && ((source.Composite1 == null && this.Composite1 == null) || (source.Composite1 != null && this.Composite1 != null && this.Composite1.Equals(source.Composite1)));
    }
  }

  public class CompositeModel {
    public string Text1 { get; set; }
    public string Constant1 { get; set; }
    public NestCompositeModel Nest1 { get; set; }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj == null)
        return false;

      var source = (CompositeModel)obj;
      return this.Text1 == source.Text1 && this.Constant1 == source.Constant1
        && ((source.Nest1 == null && this.Nest1 == null) || (source.Nest1 != null && this.Nest1 != null && this.Nest1.Equals(source.Nest1)));
    }
  }

  public class NestCompositeModel {
    public string Text1 { get; set; }

    public override int GetHashCode() {
      return base.GetHashCode();
    }

    public override bool Equals(object obj) {
      if (obj == null)
        return false;

      var source = (NestCompositeModel)obj;
      return this.Text1 == source.Text1;
    }
  }

  public class TestConverter : ICsvConverter<TestModel> {
    public string ObjectToCsv(object value) {
      return value == null ? string.Empty : ((DateTime)value).ToString("yyyy/MM/dd");
    }

    public object CsvToObject(string value) {
      return value == null ? DateTime.MinValue : DateTime.Parse(value);
    }
  }
}
