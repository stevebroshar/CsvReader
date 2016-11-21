# A CSV reader

There are so many CSV readers in the world but none are quite right. Many try to do too much making them too specialized and therefore not useful in general contexts.  Some are overly complicated to use.  Some run slow and some just don't work right. As an example, Microsoft's TextFieldParser is pretty good, but has some fatal flaws.  It ignores blank lines in a quoted value.  And, it strips whitespace from quoted values.  And, it's relatively slow.  Other than that it's great ;)

Sometimes reading a CSV is a simple as s.Split(",").  If that's all you need, then do that. Some people can get away with splitting using a regular expression.  But, as far as I know, if you need to read arbitrary complicated CSV data -- that might use all of its layout capabilities, then you need to use code of modest size and complexity -- such as this.

This implementation is intended to be fast and correct.  Of course, since there is no standard for CSV, correct is somewhat subjective. This is written to conform to the obvious aspects of CSV and attempts to provide useful behavior for aspects that are less specified.

The code is small enough to fit into a modestly sized file.  So ... at least for now ... I'm thinking of not bothering with creating a package -- with DLLs for one or more .NET frameworks. Just include the source file into your project.

## RFC4180

Of the 7 rules defined in RFC4180, this does attempt to implement them fully -- except where the RFC is confusing or seems wrong.  Specifically:

 1. *Each record is located on a separate line, delimited by a line break (CRLF).*  
 Well, sortof. A new line terminates a record -- unless it's within a quoted value. So, records never share a line, but a record can span multiple lines.

 2. *The last record in the file may or may not have an ending line break.*  
 Yep!  And further, any blank line is ignored ... unless it's within a quoted value.

 3. *There maybe [sic] an optional header line appearing as the first line of the file with the same format as normal record lines.  This header will contain names corresponding to the fields in the file and should contain the same number of fields as the records in the rest of the file (the presence or absence of the header line should be indicated via the optional "header" parameter of this MIME type).*
YEP.  But, when reading, there's no difference between the header and other records. So, there's no behavior related to this rule.  If the first line is a header, then the first record will be the header values. If not, then the first read will be the first data record.

 4. This one is actually several separate rules, so I'll break it down:

 a. *Within the header and each record, there may be one or more fields, separated by commas.*  
As this is the heart of CSV it seems to me that it should appear earlier than 4.  And, maybe clarify the wording as something like: Each record (including the header) consists of values separated by a comma.

 b. *Each line should contain the same number of fields throughout the file.*
What does 'should' mean/imply? Is the entire file invalid if all records don't have the same number of values?  That seems exteme ... well it's the sort of extreme rule you fine in XML, but CSV tends to be less severe.  For example, Excel reads a CSV file with different number of values per line.  So, this class simply returns the number of values that each record has.  FWIW, maybe this rule is a suggestion for writing CSV.  For example, Excel always writes a CSV file with the same number of values per line.

 c. *Spaces are considered part of a field and should not be ignored.*  
Does this by default, but since it seems pretty common that values are trimmed of whitespace (from the beginning and end) this class provides an option to do this.  Always ignores whitespace outside (before and after) the quotes of a quoted value.  And, propagates an exception if finds non-whitespace text outside the quotes.

 d. *The last field in the record must not be followed by a comma.*   
I don't get this.  The definition of the comma (as a separator) means that if a record ends with a comma, then the last field is a blank string.  Maybe this rule is clarification -- emphasizing that comma is not a terminator.

 5. *Each field may or may not be enclosed in double quotes (however some programs, such as Microsoft Excel, do not use double quotes at all).  If fields are not enclosed with double quotes, then double quotes may not appear inside the fields.*  
Actually, Excel does use double quotes for values that contain comma or quote.  Maybe an older version of Excel didn't.  Also, I didn't understand the need for this rule at first.  My implementation could easily handle a value that is not quoted, yet contains quotes in the middle (not in first position).  But, if a value is allowed to contain a quote, then you can't assume the quote won't be in the first position -- which signals a quoted value.  So, this rule is about ensuring consistent behavior for consistent input.

 6. *Fields containing line breaks (CRLF), double quotes, and commas should be enclosed in double-quotes.*  
Should?  I say must.

 7. *If double-quotes are used to enclose fields, then a double-quote appearing inside a field must be escaped by preceding it with another double quote.*  
CHECK

Lastly, some of the wording of the RFC is confusing to me.  It uses *fields*, but I call them *values* ... I guess they can both apply.  It uses *file*, but the source may not be a file at all.  The source is a stream or document (to steal a word from XML).

## Features Not Covered by the RFC

There are several CSV parsing/reading features that beg to be considered:

### Custom Delimiters
Why would someone want to use a delimiter other than comma ... for something called "COMMA separated values"?  Well, tab is somewhat common.  And, the implementation is easy and the performance impact is none.  So why not?  See SetDelimiters().

### Comment Lines
There is no mention of comment lines in the pseudo-official documents about CSV. But there's plenty of talk about people using comment lines.  So, it seems the people want this feature. So, why not?  I was able to add it with very minimal performance impact.  See SetCommentChars().

### Trim Whitespace
RFC4180 says that whitespace should not be trimmed and this is default behavior.  But, this class provides options to trim -- unquoted values, quoted values or both.  See TrimUnquotedValues, TrimQuotedValues and TrimValues.

### Quote in Unquoted Value
I wonder if someone might considering using this, but want to _not_ treat quotes special -- to eliminate the quoted values feature.  Thing is, without quoted values, you can just split on comma.  You don't need anything more complicated.

### References
Some documents that describe the CSV format:
 - https://en.wikipedia.org/wiki/Comma-separated_values
 - https://tools.ietf.org/html/rfc4180
 - http://www.computerhope.com/issues/ch001356.htm
