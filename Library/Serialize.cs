using System;
using System.Collections.Generic;
using System.Text;

namespace Library
{
    class Serialize
    {
        // ASCII 28 (0x1C) File Separator - Used to indicate separation between files on a data input stream.
        // ASCII 29 (0x1D) Group Separator - Used to indicate separation between tables on a data input stream(called groups back then).
        // ASCII 30 (0x1E) Record Separator - Used to indicate separation between records within a table(within a group).  These roughly map to a tuple in modern nomenclature.
        // ASCII 31 (0x1F) Unit Separator - Used to indicate separation between units within a record.The roughly map to fields in modern nomenclature.
    }
}
