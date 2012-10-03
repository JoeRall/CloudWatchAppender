A CloudWatch appender for [log4net](http://logging.apache.org/log4net/ "log4net"). You can configure log4net to direct some of you log events to [CloudWatch](http://aws.amazon.com/CloudWatch/ "CloudWatch"), making them show up as data points on a graph.

All posts are made asynchronously to CloudWatch via the [AWSSDK](http://aws.amazon.com/sdkfornet/) library for .NET.

# Installation

To add CloudWatchAppender to your Visual Studio (>2010) project, run the following command in <a href="http://docs.nuget.org/docs/start-here/using-the-package-manager-console">Package Manager Console</a> or download the source from [GitHub](https://github.com/camitz/CloudWatchAppender).

<div class="nuget-badge">
<p>
<code>PM&gt; Install-Package CloudWatchAppender</code>
</p>
</div>

# Configuration

CloudWatchAppender is easy to configure in web.config or app.config. If you've been using log4net you, probably already have the section and some of the elements defined. The following will divert all your logs events to CloudWatch. It will send a value of 1 with unit "Count". The default metric name as well as namespace is "CloudWatchAppender".

    <log4net>
        <appender name="CloudWatchAppender" type="CloudwatchAppender.CloudwatchAppender, CloudwatchAppender">
			<accessKey value="YourAWSAccessKey" />
			<secret value="YourAWSSecret" />
			<endPoint value="url or system name like: eu-west-1" />
        </appender>


        <!-- As of version 1.3, this is no longer necessary.
        <logger name="Amazon">
          <level value="OFF" />
        </logger>
        -->

        <root>
          <appender-ref ref="CloudWatchAppender" />
        </root>
    </log4net>

CloudWatchAppender can do more than this. Pretty much anything you can post to CloudWatch via the AWSSDK is supported by the appender in different ways. <strike>A notable exception is timestamp which will of course be supported in the near future.</strike> **Update!** *Supported as of version 1.2.*

To change the default behavior, you can either provide the info in the config-file or as part of the log event message. The former is cleaner in that the code can remain agnostic to the log event endpoint. The latter provides more power and granularity over what is posted. Other appenders that you might have added will the simply output the data as if it were any old message.

**Warning** Make sure you read about CloudWatch [pricing](http://docs.amazonwebservices.com/AmazonCloudWatch/latest/DeveloperGuide/cloudwatch_concepts.html) so you'll not get any surprises. Particularly, CloudWatch treats each combination of metric name, namespace and dimension as a different custom metric. You have just 10 free custom metrics. After that they start charging you.

## Config-file

**Note!** *Changes as of version 1.3:* Before crendentials and endpoint was provided in AppSettings, a small but vital element that I had overlooked and hence failed to implement properly or even document. It is essential that you put it in your config file. AppSettings still work, of course, for backwards compatibility. As an EndPoint you can provide both the system name as below, or the full url, for instance https://monitoring.eu-west-1.amazonaws.com.

The following example will post a metric with [unit](http://docs.amazonwebservices.com/AmazonCloudWatch/latest/APIReference/API_MetricDatum.html) Milliseconds and the value 20 for all loggers using this appender. The metric name will be ProcessingTime and the namespace MyApp/Process.

    <appender name="CloudWatchAppender" type="CloudwatchAppender.CloudwatchAppender, CloudwatchAppender">
		<accessKey value="YourAWSAccessKey" />
		<secret value="YourAWSSecret" />
		<endPoint value="eu-west-1" />

        <unit value="Milliseconds"/>
        <value value="20"/>
        <metricname value="ProcessingTime"/>
        <namespace value="MyApp/Processor"/>

	    <rateLimit value="20"/>

        <dimension0 type="Amazon.CloudWatch.Model.Dimension">
            <name value="InstanceID"/>
            <value value="%metadata{instanceid}"/>
        </dimension0>
    </appender>

This normally goes in your app.config or web.config. log4net allows any old xml file to be brought in and this is of course fine for CloudWatchAppender too.

Notice also we've provided the first of ten possible ["dimensions"](#dimensions). You can specify any string you like for name and value of the dimension. Above, however, we're using a special token for the value, "%metadata{instanceid}". This will be translated to the EC2 instance ID, see [Instance Metadata](#metadata) below. In fact, any token recognized by any regular PatternLayout conversion [patterns](http://logging.apache.org/log4net/release/sdk/log4net.Layout.PatternLayout.html), for instance %logger, will be translated as such.

The *rateLimit* limits the number of requests sent to CloudWatch to 20 per second. 

The exact same can be accomplished by using the ["PatternLayout"](#patternlayout) we provide and using the format rules outlined below to format the input string to the appender.

      <layout type="CloudWatchAppender.PatternLayout, CloudWatchAppender">
           <conversionPattern value="%message Value: 20 Milliseconds, MetricName: ProcessingTime, NameSpace: MyApp/Processor, Dimension0: InstanceID: %metadata{instanceid}"/>
      </layout>

## Event log message

If you pass a string to the logger like this

    ILog log = LogManager.GetLogger(typeof(MyClass));
    log.Info("These eight words will be ignored by CloudWatchAppender. Value: 20 Milliseconds, MetricName: ProcessingTime " +
			 "these seven words will be ignored too NameSpace: MyApp/Processor, Dimension0: InstanceID: %metadata{instanceid}");

most of it will be ignored by the CloudWatchAppender. Of course, if there are other appenders listening on the logger, they will handle the string in their way. Most will output the entire string to whatever end point they are designed for.

The above message to the logger will behave in the same way as the previous examples. The CloudWatchAppender event parser will look for recognizable tokens, largely corresponding to the entities and units familiar to CloudWatch. The parser is pretty lenient in what is allowed but also let's unexpected input slip without warning. As the messages get complicated, especially if PatternLayout is used, it is important to understand the conflict resolution [rules](#conflicts).

Note that if the PatternLayout is used, either the original one or the one provided by CloudWatchAppender, the pattern %message or %m (the short hand is deprecated) must be present in the conversion pattern for the event log message to be observed.

A list of [tokens](#tokens) supported by the event parser can be found below. 

### The MetricDatum object

log4net permits not just strings as the parameter to the log event method. Any old object will do and normally this it the ToString() method is used to convet it to a string. First, however, log4net will look to see if an ObjectRenderer has been registered. CloudWatchAppender automatically registers the MetricDatumRenderer, allowing you to provide a MetricDatum.

The MetricDatum comes in two flavours, the AWS CloudWatch one and the wrapper we have designed. You can use either but our one offers a few more features. Noteably it takes a string to the constructor with which you can provide info that you want output to appenders other than CloudWatchAppender that you might have registered.

Example:

    log.Info(new CloudWatchAppender.MetricDatum("A tick!")
        .WithTimestamp(DateTimeOffset.Now.AddMinutes(-10))
        .WithUnit("Kilobytes")
        .WithValue(29.4));

or

    log.Info(new Amazon.CloudWatch.Model.MetricDatum()
        .WithTimestamp(DateTime.UtcNow.AddMinutes(-10))
        .WithUnit("Kilobytes")
        .WithValue(29.4));

The above will do the same except for the message "A tick!" which if you have for instance a FileAppender cannot be output with the latter.

## <a id="configoverrides"></a>ConfigOverrides

Normally input given in parameters to the appenders will override anything given in the log event message, including anything given in the conversion pattern. To change this behaviour, add

    <configOverrides value="false"/>

to the appender definition. Now, parameters to the appender will act as defaults, should the values be missing from the message.

# Timestamp

As of version 1.2 the timestamp can be set. If left out, CloudWatch will just default it to the current time in UTC. 

Set it in your config like so if you use a locale where this format is parsable:

    <timestamp value="2012-09-06 14:00:00 +02:00"/>

It works, of course, but you'll find this more useful.

    <timestamp value="%utcdate{DATE}"/>

The above uses the log4net PatterConverted for a DateTime-formatted current UTC timestamp. Both the below are permissible in the eventlog message:

    "Timestamp: 2012-09-06 12:00:00"
    
    "Timestamp: %date{DATE}"

Both above will be assumed to be UTC, so the second one, at least, may not have expected result. Use utcdate to avoid confusion.

It is recommended to think UTC. That's what CloudWatch does. DateTime.UtcNow will get you the current UTC timestamp. In fact, CloudWatch refuses to display anything in what it deems to be the future, which can be confusing, particularly if you're on the plus side of UTC. You can read about my [experiences](http://blog.simpletask.se/awscloudwatch-log4net-appender) prior to figuring that one out on my blog.

# Statistics

As well as singles values with unit, CloudWatchAppender supports statistics. This is useful when sending single point values becomes too burdensome. Instead, you aggregate your data to be sent every minute or so. The CloudWatchAppender doesn't go the aggregation for you, you'll have to take care of that yourself. Todo? ;).

Statistics are posted by specifying Minimum, Maximum, Sum and SampleCount. (The average is calculated by CloudWatch as Sum/SampleCount.) These entities are all recognized by CloudWatchAppender and used as above, either in the event log message or in the config file.

The following behave the same way.

    <unit value="Milliseconds"/>
    <sum value="3000"/>
    <minimum value="3"/>
    <minimum value="400"/>
    <samplecount value="250"/>

    log.Info("unit: milliseconds, sum: 3000, minimum: 3, minimum: 400, samplecount: 250");
    

# <a id="dimensions"></a>Dimensions

CloudWatch supports up to 10 dimensions given as name/value pair. CloudWatchAppender has no limit but don't try exceeding 10.

**Warning** Again, make sure you understand that overusing dimensions can quickly become expensive.

In your config file under the appender element you can add dimensions simply by listing a bunch of elements like this:

      <dimension type="Amazon.CloudWatch.Model.Dimension">
        <name value="InstanceID"/>
        <value value="%metadata{instanceid}"/>
      </dimension>

      <dimension type="Amazon.CloudWatch.Model.Dimension">
        <name value="Fruit"/>
        <value value="Apple"/>
      </dimension>

Again, note the [metadata](#metadat) pattern.

The corresponding equivalent pattern/event log message would be

    "Dimension: InstanceID: %metadata{instanceid}, Dimension: Fruit: Apple" //(%metadata{instanceid} only parsed if in a layout conversion pattern)

or simply

    "Dimensions: (InstanceID: %metadata{instanceid}, Fruit: Apple)" //(%metadata{instanceid} only parsed if in a layout conversion pattern)

Deppending on the setting of [ConfigOverrides](#configoverrides) individual dimensions compete according to name (key). Internally in CloudWatchAppender the dimensions are stores as a dictionary.

# <a id="patternlayout"></a> Using layouts and the CloudWatchAppender PatternLayout

The CloudWatchAppender supports layouts as expected. The most commonly used by far is PatternLayout. You can use the original PatternLayout but it is recommended to use the one provided in the CloudWatchAppender library.

The CloudWatchAppender PatternLayout subclasses PatternLayout so all features should work as expected. Plus there are a few extran functions. Any [patterns](http://logging.apache.org/log4net/release/sdk/log4net.Layout.PatternLayout.html) supported by PatternLayout is headed. Some of them, however, will be less sensible to use in this context. Firstly, some of them take to long and secondly, some will not make sense to CloudWatch or the appender. 

## <a id="metadata"></a> Instance metadata

The pattern %metadata{*key*} anywhere in your conversion pattern will be replaced by the instance metadata indicated by the supplied key of the instance on which the application is running. This information is retrieved via an asynchronous AWS metadata API call at first use and then cached in a static field.

For example, %metadata{instanceid} will provide the instance id and may be useful as a dimension.

Providing the same token, or any other supported token for that matter, in the config-file is also possible and works as expected.

Supported keys are:

* amiid (ami-id)
* amilaunchindex (ami-launch-index)
* amimanifestpath (ami-manifest-path)
* instanceid (instance-id)
* instancetype (instance-type)
* kernelid (kernel-id)
* localhostname (local-hostname)
* localipv4 (local-ipv4)
* mac (mac)
* availabilityzone (placement/availability-zone)
* productcodes (product-codes)
* publichostname (public-hostname)
* publicipv4 (public-ipv4)
* publickeys (public-keys)
* reservationid (reservation-i)

See [Instance Metadata](http://docs.amazonwebservices.com/AWSEC2/latest/UserGuide/AESDG-chapter-instancedata.html).

The old pattern %instanceid is deprecated, but still works.

If you download the source code there is a tiny utility that tests all these keys on your system.

## Augmented %logger functionality

The log4net PatternLayout provides the %logger or %c pattern, which translates to the name of the logger, typically the qualified name of the type issuing the event. You can also specify a precision specifier as a integer enclosed in curly brackets following the pattern, to filter out the end part of the name. 

CloudWathcAppender adds support for negative precision specifiers, filtering out the beginning part of the name. This is useful for providing the type and namespace as metric name and namespace respectively to CloudWatch.

Typically you'd create the logger like so.

    ILog log = LogManager.GetLogger(typeof(MyClass));

If the namespace is MyApp.MyNamespace, then *MetricName: %logger{2}* in your pattern would post "MyNamespace.MyClass" as the metric name to CloudWatch. The following may be more suitable for your needs.

    MetricName: %logger{1}, NameSpace: %logger{-1}

The negative precision specifier removes the last word from the name. The metric name will now by MyClass and the namespace will be MyApp.MyNamespace.

Dots (.) are converted to slashes (/) before sending. This is true of all strings passed as namespace anywhere.

## RateLimit

Placing the following in the appender config, inhibits the number of request sent to CloudWatch.

	<rateLimit value="20"/>

A maximum of 20 per second is sent. This is useful for logging errors which could suddenly spike. The default is not to limit, but it is recommended you do.

Any events exceeding the rate limit will not be processed by the appender. Other appenders my still process the event, of course.

## <a id="conflicts"></a> Handling conflicting input

TODO

# <a id="tokens"></a> Tokens recognized by CloudWatchAppender

## Units

* Seconds
* Microseconds
* Milliseconds
* Bytes
* Kilobytes
* Megabytes
* Gigabytes
* Terabytes
* Bits
* Kilobits
* Megabits
* Gigabits
* Terabits
* Percent
* Count
* Bytes/Second
* Kilobytes/Second
* Megabytes/Second
* Gigabytes/Second
* Terabytes/Second
* Bits/Second
* Kilobits/Second
* Megabits/Second
* Gigabits/Second
* Terabits/Second
* Second
* None

## Metrics

* Value
* Unit
* Dimension
* Dimensions
* NameSpace
* MetricName
* Timestamp

## Statistics

* Maximum
* Minimum
* SampleCount
* Sum

# Some more reading

Check out the following blog posts that seeded the project.

[A CloudWatch appender for log4net](http://blog.simpletask.se/awscloudwatch-log4net-appender)

[Improving the CloudWatch Appender](http://blog.simpletask.se/improving-cloudwatch-appender)

# Releases

## 2.2 <font size="2">(beta) 2012-10-02 </font>

### Performance enhancements

* Up to 200 % faster on base case i.e. log.Info("") and minimal config.

## 2.1.1 <font size="2">(beta) 2012-10-02 </font>

### Bug fixes

* Zeroing out dimensions in config didn't work
* Exceptions and timeouts not well handled, potentially could crash the iis worker
* Backwards compatibility for instance id didn't work

## 2.1.0 <font size="2">(beta) 2012-09-24 </font>

### New features

* Rate limit (rateLimit)
* More instance metadata options (former instanceid pattern deprecated)
* Instance metadata reader async

## 2.0.1 <font size="2">(beta) 2012-09-13 </font>

* Essential bugfix.

## 2.0.0 <font size="2">(beta) 2012-09-13 </font>

### Breaking changes

* Due to confusion with the appender Name property, the alias Name for MetricName will is longer be supported.

### Bug fixes

* Patterns in dimension values weren't properly parsed.

## 1.3.2 <font size="2">(beta) 2012-09-12</font>

### Bug fixes

* Backward compatibility (credentials in AppSettings) was broken in 1.3.1.

## 1.3.1 <font size="2">(beta) 2012-09-12</font>

### Bug fixes

* A layout was required in config.
* Default timestamp didn't work. Prevented requests.

## 1.3 <font size="2">(beta) 2012-09-11</font>

### New features 

* Credentials and EndPoint can now be provided in the appender config.
* MetricDatum (both variants) supported in call to log event method.
* Eliminiting debug output from AWSSDK in config is no longer necessary.

### Bug fixes

* Credentials and EndPoint needed to be set in AppSettings. Undocumented.

## 1.2 <font size="2">(beta) 2012-09-06</font>

### New features 

* MetricDatum timestamp property supported. Current date format provider used for parsing, UTC assumed. For example Timestamp: <nobr>2012-09-06 14:00:00 +02:00</nobr>.

### Bug fixes

* Bug in AWSSDK concerning decimal separator in locales using for example "," instead of ".". Hacked by temporarily setting default locale.

## 1.1 (beta)

### New features 

* Intance id is cached. 
* . to / conversion for all NameSpace strings in MetricDAtum.
* Support for lowercase units. 
* Ceased support for ordered dimensions 

### Bug fixes

* ConfigOverrides false wasn't checked for value 
* ConfigOverrides didn't work as expected
* Couldn't override dimensions (error)

## 1.0 (beta)

[![endorse](http://api.coderwall.com/camitz/endorsecount.png)](http://coderwall.com/camitz)