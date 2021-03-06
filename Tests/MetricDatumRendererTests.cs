using System;
using System.Collections.Generic;
using System.IO;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using CloudWatchAppender.Model;
using NUnit.Framework;

namespace CloudWatchAppender.Tests
{
    [TestFixture]
    public class MetricDatumRendererTests
    {

        [Test]
        public void AmazonMetricDatum()
        {
            var t = new StringWriter();

            new MetricDatumRenderer().RenderObject(null, new Amazon.CloudWatch.Model.MetricDatum
                                                             {
                                                                 MetricName = "TheMetricName",
                                                                 Unit = "Seconds",
                                                                 Value = 5.1,
                                                                 Timestamp = DateTime.Parse("2012-09-11 11:11"),
                                                                 Dimensions = new List<Dimension>
                                                                                  {
                                                                                      new Dimension
                                                                                          {
                                                                                              Name = "dim1",
                                                                                              Value = "v1"
                                                                                          },
                                                                                      new Dimension
                                                                                          {
                                                                                              Name = "dim2",
                                                                                              Value = "v2"
                                                                                          }
                                                                                  }
                                                             }, t);



            Assert.That(t.ToString(), Is.StringContaining("MetricName: TheMetricName"));
            Assert.That(t.ToString(), Is.StringContaining("Unit: Seconds"));
            Assert.That(t.ToString(), Is.StringContaining("Value: 5.1"));
            Assert.That(t.ToString(), Is.StringContaining("Timestamp: 9/11/2012 11:11"));
            Assert.That(t.ToString(), Is.StringContaining("Dimensions: dim1: v1, dim2: v2"));
        }

        [Test]
        public void AmazonMetricDatum_WithStatistics()
        {
            var t = new StringWriter();

            new MetricDatumRenderer().RenderObject(null, new Amazon.CloudWatch.Model.MetricDatum
                                                             {
                                                                 StatisticValues = new StatisticSet
                                                                                       {
                                                                                           Maximum = 100.1,
                                                                                           Minimum = 2.1,
                                                                                           Sum = 250.1,
                                                                                           SampleCount = 4
                                                                                       }
                                                             }, t);

            Assert.That(t.ToString(), Is.StringContaining("Maximum: 100.1"));
            Assert.That(t.ToString(), Is.StringContaining("Minimum: 2.1"));
            Assert.That(t.ToString(), Is.StringContaining("Sum: 250.1"));
            Assert.That(t.ToString(), Is.StringContaining("SampleCount: "));
        }

        [Test]
        public void AppenderMetricDatum_WithStatistics()
        {
            var t = new StringWriter();

            new MetricDatumRenderer().RenderObject(null, new Model.MetricDatum()
                                        .WithStatisticValues(new StatisticSet
                                                                 {
                                                                     Maximum = 100.1,
                                                                     Minimum = 2.1,
                                                                     Sum = 250.1,
                                                                     SampleCount = 4
                                                                 }),
                                     t);

            Assert.That(t.ToString(), Is.StringContaining("Maximum: 100.1"));
            Assert.That(t.ToString(), Is.StringContaining("Minimum: 2.1"));
            Assert.That(t.ToString(), Is.StringContaining("Sum: 250.1"));
            Assert.That(t.ToString(), Is.StringContaining("SampleCount: "));
        }
    }
}