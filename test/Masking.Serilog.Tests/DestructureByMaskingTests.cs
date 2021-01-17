﻿using System;
using System.Linq;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using System.Collections.Generic;
using Masking.Serilog.Tests.Infrastructure;
using Masking.Serilog.Tests.Models;

namespace Masking.Serilog.Tests
{
    [TestFixture]
    public class DestructureByMaskingTests
    {
        [Test]
        public void ClassWithAPropertyOnlyWithSetterDoesNotCrash()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties("Name", "Password")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var ignored = new DestructureMeWithPropertyWithOnlySetter
            {
                Id = 2,
                Name = "Name",
                Password = "Password"
            };

            log.Information("Here is {@Ignored}", ignored);

            Assert.IsTrue(true, "We did not throw!");
        }

        [Test]
        public void ComplexTypesAreMaskedWhenDestructuring()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties(opts =>
                {
                    opts.PropertyNames.Add("Hash");
                    opts.Mask = "*removed*";
                })
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var ignored = new Complex
            {
                HashData = new DestructMe
                { Hash = 1234 }
            };

            log.Information("Here is {@Ignored}", ignored);

            var props = evt.GetProps("Ignored");
            var hashData = ((StructureValue)props[nameof(Complex.HashData)]).Properties.ToDictionary(p => p.Name, p => p.Value);

            Assert.AreEqual("*removed*", hashData[nameof(DestructMe.Hash)].LiteralValue());
        }

        [Test]
        public void PropertyNamesAreMaskedWhenDestructuring()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties("password", "secret")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            DestructureMe.StaticProp = 1337;

            var ignored = new DestructureMe
            {
                Id = 2,
                Name = "Name",
                Password = "Password",
                Secret = 25673433
            };

            log.Information("Here is {@Ignored}", ignored);

            var props = evt.GetProps("Ignored");

            Assert.AreEqual(2, props[nameof(DestructureMe.Id)].LiteralValue());
            Assert.AreEqual("Name", props[nameof(DestructureMe.Name)].LiteralValue());
            Assert.AreEqual("******", props[nameof(DestructureMe.Password)].LiteralValue());
            Assert.AreEqual("******", props[nameof(DestructureMe.Secret)].LiteralValue());
            Assert.AreEqual(1337, props[nameof(DestructureMe.StaticProp)].LiteralValue());
        }

        [Test]
        public void PropertyNamesAreMaskedWhenDestructuringStruct()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties("HASH")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var ignored = new DestructMe
            {
                Id = 2,
                Hash = 25673433
            };

            log.Information("Here is {@Ignored}", ignored);

            var props = evt.GetProps("Ignored");

            Assert.AreEqual(2, props[nameof(DestructMe.Id)].LiteralValue());
            Assert.AreEqual("******", props[nameof(DestructMe.Hash)].LiteralValue());
        }

        [Test]
        public void IntIndexedPropertyNamesDoesNotBreakWhenDestructuring()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties("password", "secret")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var data = new IntIndexed();
            data[0] = "boo";

            log.Information("Here is {@data}", data);

            var props = evt.GetProps("data");

            Assert.IsNull(props["Item"].LiteralValue());
        }

        [Test]
        public void StringIndexedPropertyNamesDoesNotBreakWhenDestructuring()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties("password", "secret")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var data = new StringIndexed();
            data["woo"] = "boo";

            log.Information("Here is {@data}", data);

            var props = evt.GetProps("data");

            Assert.IsNull(props["Item"].LiteralValue());
        }

        [Test]
        public void PropertyNamesOfTypesInCollectionsAreMaskedWhenDestructuring()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties("password", "secret")
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var ignored = new[] {
                new DestructureMe
                {
                    Id = 2,
                    Name = "Name",
                    Password = "Password",
                    Secret = 25673433
                }
            };

            log.Information("Here is {@Ignored}", ignored);

            var props = evt.GetProps("Ignored");

            Assert.AreEqual(2, props[nameof(DestructureMe.Id)].LiteralValue());
            Assert.AreEqual("Name", props[nameof(DestructureMe.Name)].LiteralValue());
            Assert.AreEqual("******", props[nameof(DestructureMe.Password)].LiteralValue());
            Assert.AreEqual("******", props[nameof(DestructureMe.Secret)].LiteralValue());
        }

        [Test]
        public void ValuesOfStaticPropertiesAreNotIncluded()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties(opts =>
                {
                    opts.ExcludeStaticProperties = true;
                    opts.PropertyNames.AddRange(new[] { "password", "secret" });
                })
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            DestructureMe.StaticProp = 1337;

            var ignored = new DestructureMe
            {
                Id = 2,
                Name = "Name",
                Password = "Password",
                Secret = 25673433
            };

            log.Information("Here is {@Ignored}", ignored);

            var props = evt.GetProps("Ignored");

            Assert.AreEqual(2, props[nameof(DestructureMe.Id)].LiteralValue());
            Assert.AreEqual("Name", props[nameof(DestructureMe.Name)].LiteralValue());
            Assert.AreEqual("******", props[nameof(DestructureMe.Password)].LiteralValue());
            Assert.AreEqual("******", props[nameof(DestructureMe.Secret)].LiteralValue());
            Assert.IsFalse(props.ContainsKey(nameof(DestructureMe.StaticProp)), $"{nameof(props)} contains the key {nameof(DestructureMe.StaticProp)}.");
        }


        
        [Test]
        public void PropertyNamesOfTypesInCollectionsAreNotMaskedIfTheNameSpaceIsIgnoredWhenDestructuring()
        {
            LogEvent evt = null;

            var log = new LoggerConfiguration()
                .Destructure.ByMaskingProperties(opts =>
                {
                    opts.PropertyNames.Add("password");
                    opts.PropertyNames.Add("secret");
                    opts.IgnoredNamespaces.Add("Masking.Serilog.Tests.Models");
                })
                .WriteTo.Sink(new DelegatingSink(e => evt = e))
                .CreateLogger();

            var ignored = new[] {
                new DestructureMeButIgnored
                {
                    Id = 2,
                    Name = "Name",
                    Password = "Password",
                    Secret = 25673433
                }
            };

            log.Information("Here is {@Ignored}", ignored);

            var props = evt.GetProps("Ignored");

            Assert.AreEqual(2, props[nameof(DestructureMeButIgnored.Id)].LiteralValue());
            Assert.AreEqual("Name", props[nameof(DestructureMeButIgnored.Name)].LiteralValue());
            Assert.AreEqual("Password", props[nameof(DestructureMeButIgnored.Password)].LiteralValue());
            Assert.AreEqual(25673433, props[nameof(DestructureMeButIgnored.Secret)].LiteralValue());
        }
    }
}
