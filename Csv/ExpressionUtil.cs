using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Csv {
  public class ExpressionUtil {
    /// <summary>
    /// プロパティを指定したラムダ式からMemberExpressionを取得。
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static MemberExpression GetMemberExpression(LambdaExpression expression) {
      if (expression.Body is MemberExpression) {
        return (MemberExpression)expression.Body;
      }
      else if (expression.Body is UnaryExpression) {
        var unary = (UnaryExpression)expression.Body;
        return (MemberExpression)unary.Operand;
      }

      throw new InvalidOperationException();
    }

    /// <summary>
    /// プロパティを指定したラムダ式からプロパティチェインの情報を取得。
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public static IDictionary<string, PropertyDescriptor> GetPropertyChain(LambdaExpression expression) {
      var result = new Dictionary<string, PropertyDescriptor>();
      var memberExpression = ExpressionUtil.GetMemberExpression(expression);
      while (memberExpression != null) {
        var properties = TypeDescriptor.GetProperties(memberExpression.Member.DeclaringType);
        result.Add(memberExpression.Member.Name, properties.Find(memberExpression.Member.Name, false));
        memberExpression = memberExpression.Expression as MemberExpression;
      }

      return result;
    }
  }
}
