A CSV reader.  

There are so many CSV readers in the world and none of them seem to work right.
Many try to do too much making them too specialized and therefore not useful in general 
contexts.  Some are overly complicated to use.  Some run slow.  As an example, Microsoft's 
TextFieldParser is pretty good, but has some fatal flaws.  It ignores blank lines in a quoted
value.  And, it strips whitespace from quoted values.  And, it's relatively slow.  Other than
that it's great ;)

This implementation is intended to be fast and correct.  Of course, since there is no 
standard for CSV, correct is somewhat subjective.  This is written to conform to the 
obvious aspects of CSV and supports switches to control some of the more contentious 
behaviors ... such as:

## Custom Delimiters
Why would someone want to use a delimiter other than comma ... for something called "COMMA
separated values"?  Well, tab is somewhat common.  And, the implementation is easy and the 
performance impact is none.  So why not?  See SetDelimiters().

## Comment Lines
There is no mention of comment lines in the psuedo-official documents about CSV. But there's
plenty of talk about people using comment lines.  So, it seems the people want this feature.
So, why not?  I was able to add it with very minimal performance impact.  See 
SetCommentChars().

## Trim Whitespace [TODO: implemented this]
RFC4180 says that whitespace should not be trimmed.  But, consider the quoted value.  Does
that mean the whitespace before and after the quotes should be included?  That's nonesense!
I'd say RFC4180 is incomplete if not wrong WRT quoted values.  But, should the whitespace be
trimmed for unquoted values?  Hmm.  Who knows?  Let's make it optional.  I'm picking trimmed
as default since it makes sense to me. Sorry RFC4180.

## Quote in Unquoted Value
RFC4180 says that an unquoted value should NOT contain a quote so I added checking -- 
propagating an exception if found.  But, I wonder whether consumers might sometimes like to
relax that rule.  It's easy to not treat a quote as special when the value is not 
enclosed in a quote -- starts with a quote -- just don't check for a quote char.  But, I 
think the issue is robustness.  If the consumer is expecting quotes to not be special for 
an unquoted value, but has a value with a quote at the beginning, then there is ambiguity.
The consumer might think the value should be read with the quote, but since quoted value 
is so core to CSV, the value would be parsed as quoted.  hmm.  Maybe there should be an 
option to disable quoted value parsing.  Hey maybe that's what 
TextFieldParser.HasFieldsEnclosedInQuotes is about.  Maybe this should have a similar 
switch.  I'd call it something better like SupportQuotedValues -- defaulting to true ... 
since it's normal CSV behavior.

## References
Some documents that describe the CSV format:
https://en.wikipedia.org/wiki/Comma-separated_values
https://tools.ietf.org/html/rfc4180
http://www.computerhope.com/issues/ch001356.htm
