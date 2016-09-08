import os
import sys
import re

rootdir = sys.argv[1]
files = [os.path.join(dp, f) for dp, dn, filenames in os.walk(rootdir) for f in filenames if os.path.splitext(f)[1] == '.cs']
for file in files:
    content = ''
    with open(file, 'r') as fin:
        content = fin.read()
    with open(file, 'w') as fout:
        # /**, /*
        content = re.sub("\/\*\*\n", "/// <summary>\n", content)
        content = re.sub("\/\*\n", "/// <summary>\n", content)

        # */
        content = re.sub(r"([ ]+) \*\/\n", r"\1/// </summary>\n", content)

        # /** */, /** 
        content = re.sub(r"([ ]+)\/\*\*(.+)\*\/\n", r"\1/// \2\n", content)
        content = re.sub("\/\*\* ", "/// <summary> ", content)

        # * , *
        content = re.sub(r"([ ]+) \* ", r"\1/// ", content)
        content = re.sub(" \*\n", "///\n", content)

        # {@code }{@code }
        content = re.sub(r"(\{@code )([^\}]+)\}(.+)(\{@code )([^\}]+)\}", r"<code> \2</code>\3<code> \5</code>", content)

        # {@code }
        content = re.sub(r"(\{@code )([^\}]+)\}", r"<code> \2</code>", content)

        # <p>
        content = re.sub("<p>", "<para />", content)

        # <pre>, </pre>
        content = re.sub("<pre>", "", content)
        content = re.sub("</pre>", "", content)

        # {@link }
        content = re.sub(r"(\{@link )([^\}]+)\}", r'<see cref="\2"/>', content)

        # @param
        content = re.sub(r"(@param )([^ \n]+) (.+)\n", r'<param name="\2">\3</param>\n', content)
        content = re.sub(r"(@param )([^ \n]+)\n", r'<param name="\2"></param>\n', content)

        # Write file
        fout.write(content)