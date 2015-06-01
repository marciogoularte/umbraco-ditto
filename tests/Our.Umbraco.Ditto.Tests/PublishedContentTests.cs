﻿namespace Our.Umbraco.Ditto.Tests
{
    using System.Linq;

    using NUnit.Framework;
    using Our.Umbraco.Ditto.Tests.Mocks;
    using Our.Umbraco.Ditto.Tests.Models;
    using global::Umbraco.Core.Models;
    using global::Umbraco.Web;

    [TestFixture]
    public class PublishedContentTests
    {
        [Test]
        public void Name_IsMapped()
        {
            var name = "MyCustomName";

            var content = ContentBuilder.Default()
                .WithName(name)
                .Build();

            var model = content.As<SimpleModel>();

            Assert.That(model.Name, Is.EqualTo(name));
        }

        [Test]
        public void Children_Counted()
        {
            var child = ContentBuilder.Default().Build();

            var content = ContentBuilder.Default()
                .AddChild(child)
                .Build();

            //Do your Ditto magic here, and assert it maps as it should
            Assert.That(content.Children.Count(), Is.EqualTo(1));
        }

        [Test]
        public void Property_Returned()
        {
            var value = "myval";

            var property = PropertyBuilder.Default("myprop", value).Build();

            var content = ContentBuilder.Default()
                .AddProperty(property)
                .Build();

            var model = content.As<SimpleModel>();

            Assert.That(model.MyProperty, Is.EqualTo(value));
        }

        [Test]
        [ExpectedException(typeof(System.InvalidOperationException))]
        public void Property_Converted()
        {
            // With this kind of mocking, we dont need property value converters, because they would already
            // have run at this point, so we just mock the result of the conversion.

            var value = ContentBuilder.Default().Build();

            var prop = PropertyBuilder.Default("myprop", value).Build();

            var content = ContentBuilder.Default()
                .AddProperty(prop)
                .Build();

            var model = content.As<SimpleModel>();

            Assert.That(model.MyProperty, Is.Not.EqualTo(value));
            Assert.That(model.MyProperty, Is.EqualTo(value.ToString()));
        }

        [Test]
        public void Property_Resolved()
        {
            var content = ContentBuilder.Default().Build();

            var model = content.As<ComplexModel>();

            Assert.That(model.MyResolvedProperty, Is.EqualTo("Mock Property Value"));
        }

        [Test]
        public void Complex_Property_Convertered()
        {
            var value = ContentBuilder.Default().Build();

            var prop = PropertyBuilder.Default("myprop", value).Build();

            var content = ContentBuilder.Default()
                .WithId(1234)
                .AddProperty(prop)
                .Build();

            var model = content.As<ComplexModel>();

            Assert.That(model.Id, Is.EqualTo(1234));

            Assert.That(model.MyProperty, Is.EqualTo(value));

            Assert.That(model.MyPublishedContent, Is.InstanceOf<IPublishedContent>());
            Assert.That(model.MyPublishedContent.Id, Is.EqualTo(1234));
            Assert.That(model.MyPublishedContent.Name, Is.EqualTo("Mock Published Content"));
        }

        [Test]
        public void Property_AppSetting_Returned()
        {
            var value = "MyAppSettingValue";

            var content = ContentBuilder.Default().Build();

            var model = content.As<ComplexModel>();

            Assert.That(model.MyAppSettingProperty, Is.EqualTo(value));
        }

        [Test]
        public void Nested_Property_Model_Populated()
        {
            var metaTitle = PropertyBuilder.Default("metaTitle", "This is the meta title").Build();
            var metaDescription = PropertyBuilder.Default("metaDescription", "This is the meta description").Build();
            var metaKeywords = PropertyBuilder.Default("metaKeywords", "these,are,meta,keywords").Build();

            var content = ContentBuilder.Default()
                .AddProperty(metaTitle)
                .AddProperty(metaDescription)
                .AddProperty(metaKeywords)
                .Build();

            var model = content.As<CurrentContentTestModel>();

            Assert.That(model, Is.Not.Null);
            Assert.That(model.MetaData1, Is.Not.Null);
            Assert.That(model.MetaData2, Is.Null);
        }

        [Test]
        [ExpectedException(typeof(System.InvalidOperationException))]
        public void Content_To_String()
        {
            var content = ContentBuilder.Default().Build();

            var model = content.As<string>();

            Assert.That(string.IsNullOrWhiteSpace(model), Is.False);
        }
    }
}