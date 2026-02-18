using NUnit.Framework;
using System;
using System.Collections.Specialized;
using System.Web;

public class TestBreezeHelper
{
    [Test]
    public void TestUpdateUrlQueryParams_AddParamsToUrlWithoutQuery()
    {
        // Arrange
        string url = "https://example.com/path";
        var extraParams = new NameValueCollection
        {
            { "key1 k3", "value1" },
            { "key2", "value2" }
        };

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);
        UnityEngine.Debug.Log(result);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("key1+k3=value1"));
        Assert.IsTrue(result.Contains("key2=value2"));
        Assert.IsTrue(result.StartsWith("https://example.com/path"));

        var kvs = HttpUtility.ParseQueryString(new Uri(result).Query);
        var val = kvs.Get("key1 k3");
        Assert.IsTrue(val == "value1");
    }

    [Test]
    public void TestUpdateUrlQueryParams_ReplaceExistingParams()
    {
        // Arrange
        string url = "https://example.com/path?key1=oldvalue&key2=value2";
        var extraParams = new NameValueCollection
        {
            { "key1", "newvalue" }
        };

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("key1=newvalue"));
        Assert.IsTrue(result.Contains("key2=value2"));
        Assert.IsFalse(result.Contains("key1=oldvalue"));
    }

    [Test]
    public void TestUpdateUrlQueryParams_AppendOnly_AddsNewParams()
    {
        // Arrange
        string url = "https://example.com/path?existing=value1";
        var extraParams = new NameValueCollection
        {
            { "newkey", "newvalue" },
            { "existing", "value2" },
        };

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams, appendOnly: true);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("existing=value1"));
        Assert.IsTrue(result.Contains("existing=value2"));
        Assert.IsTrue(result.Contains("newkey=newvalue"));
    }

    [Test]
    public void TestUpdateUrlQueryParams_AppendOnly_KeepsExistingAndAddsDuplicate()
    {
        // Arrange
        string url = "https://example.com/path?key1=existing";
        var extraParams = new NameValueCollection
        {
            { "key1", "newvalue" }
        };

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams, appendOnly: true);

        // Assert
        Assert.IsNotNull(result);
        // Both values should be present when appending
        Assert.IsTrue(result.Contains("key1=existing") || result.Contains("existing"));
        Assert.IsTrue(result.Contains("key1=newvalue") || result.Contains("newvalue"));
    }

    [Test]
    public void TestUpdateUrlQueryParams_MultipleValuesForSameKey()
    {
        // Arrange
        string url = "https://example.com/path";
        var extraParams = new NameValueCollection
        {
            { "key1", "value1" },
            { "key1", "value2" }
        };

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("key1=value1"));
        Assert.IsTrue(result.Contains("key1=value2"));
    }

    [Test]
    public void TestUpdateUrlQueryParams_EmptyParams()
    {
        // Arrange
        string url = "https://example.com/path?existing=value";
        var extraParams = new NameValueCollection();

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("existing=value"));
    }

    [Test]
    public void TestUpdateUrlQueryParams_UrlEncoding()
    {
        // Arrange
        string url = "https://example.com/path";
        var extraParams = new NameValueCollection
        {
            { "key with & spaces", "value with & special chars" }
        };

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);
        UnityEngine.Debug.Log(result);

        // Assert
        Assert.IsNotNull(result);
        // Should be URL encoded
        Assert.IsTrue(result.Contains("key+with+%26+spaces"));
        Assert.IsTrue(result.Contains("value+with+%26+special+chars"));
    }

    [Test]
    public void TestUpdateUrlQueryParams_RemoveAllParams()
    {
        // Arrange
        string url = "https://example.com/path?key1=value1&key2=value2";
        var extraParams = new NameValueCollection
        {
            { "key1", "" }
        };

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);

        // Assert
        Assert.IsNotNull(result);
        // key1 should be updated (empty value), key2 should remain
        Assert.IsTrue(result.Contains("key2=value2"));
    }

    [Test]
    public void TestUpdateUrlQueryParams_ComplexUrl()
    {
        // Arrange
        string url = "https://example.com:8080/api/v1/products?category=electronics&sort=price";
        var extraParams = new NameValueCollection
        {
            { "sort", "name" },
            { "limit", "10" }
        };

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.StartsWith("https://example.com:8080/api/v1/products"));
        Assert.IsTrue(result.Contains("category=electronics"));
        Assert.IsTrue(result.Contains("sort=name"));
        Assert.IsTrue(result.Contains("limit=10"));
        Assert.IsFalse(result.Contains("sort=price"));
    }

    [Test]
    public void TestUpdateUrlQueryParams_NoQueryStringInResultWhenNoParams()
    {
        // Arrange
        string url = "https://example.com/path";
        var extraParams = new NameValueCollection();

        // Act
        string result = BreezeHelper.UpdateUrlQueryParams(url, extraParams);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("https://example.com/path", result);
    }
}
