# A CSV reader

There are so many CSV readers in the world and none of them seem to work right.
Many try to do too much making them too specialized and therefore not useful in general 
contexts.  Some are overly complicated to use.  Some run slow.  As an example, Microsoft's 
TextFieldParser is pretty good, but has some fatal flaws.  It ignores blank lines in a quoted
value.  And, it strips whitespace from quoted values.  And, it's relatively slow.  Other than
that it's great ;)

This implementation is intended to be fast and correct.  Of course, since there is no 
standard for CSV, correct is somewhat subjective.  This is written to conform to the 
obvious aspects of CSV and attempts to provide useful behavior for aspects that are less
specified.

Of the 7 rules defined in RFC4180, this does attempt to implement them fully -- except 
where the RFC is confusing or IMO deficient/wrong.  Specifically:

 1. *Each record is located on a separate line, delimited by a line break (CRLF).* CHECK

 2. *The last record in the file may or may not have an ending line break.*  Yep!  And further, any 
blank line is ignored ... unless it's within a value.

 3. *There maybe an optional header line appearing as the first line
of the file with the same format as normal record lines.  This
header will contain names corresponding to the fields in the file
and should contain the same number of fields as the records in
the rest of the file (the presence or absence of the header line
should be indicated via the optional "header" parameter of this
MIME type).*  YEP.  But, when reading, there's no difference between 
the header and the other lines.  So, there's no behavior realated to 
this.  If the first line is a header, then first record will be the 
header values. If not, then the first read will be the first data record.

 4. This one is actually several separate rules, so I'll break it down:

 a. *Within the header and each record, there may be one or more
fields, separated by commas.  * Isn't this the same as #1?

 b. *Each line should contain the same number of fields throughout the file.*
Well, what does 'should' mean/imply? Is the entire file invalid if all 
records don't have the same number of values?  That seems exteme ... well 
it's the sort of extreme rule you fine in XML, but CSV tends to be less
severe.  So, this class simply returns the number of values that each record 
has.

 c. *Spaces are considered part of a field and should not be ignored.*  I don't
know about this one.  It seems pretty common that values are trimmed of Whitespace
from the beginning and end.  But, the thing I really wonder about is quoted values.
If whitespace is found before or after the enclosing quotes of a value, should that 
be included in the value?  I think not!  Therefore, I have to doubt this rule
altogether.

 d. *The last field in the record must not be followed by a comma.*   What?  If the
last value is blank, then the record ends in a comma.  I guess this may be related
to enforcing the same number of values per record.

 5. *Each field may or may not be enclosed in double quotes (however
some programs, such as Microsoft Excel, do not use double quotes
at all).  If fields are not enclosed with double quotes, then
double quotes may not appear inside the fields.*  CHECK

 6. *Fields containing line breaks (CRLF), double quotes, and commas
should be enclosed in double-quotes.*  Should?  I would say must.

 7. *If double-quotes are used to enclose fields, then a double-quote
appearing inside a field must be escaped by preceding it with
another double quote.*  CHECK

# Features Not Covered By Standard

There are several CSV parsing/reading features that beg to be considered:

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

## Quote in Unquoted Value [TODO]
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
 - https://en.wikipedia.org/wiki/Comma-separated_values
 - https://tools.ietf.org/html/rfc4180
 - http://www.computerhope.com/issues/ch001356.htm
