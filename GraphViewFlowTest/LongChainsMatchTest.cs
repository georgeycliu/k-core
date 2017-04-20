﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphViewFlowTest
{
    class LongChainsMatchTest:PatternMatchTest
    {
        public override string getQueryString()
        {
            return @"set transaction isolation level read uncommitted;
                SELECT count(*)
                FROM 
                    Patent_NT as A, 
                    Patent_NT as B, 
                    Patent_NT as C, 
                    Patent_NT as D,
                    Patent_NT as E,
                    Patent_NT as F
                MATCH 
                    A-[adjacencyList as E1]->B-[adjacencyList as E2]->C-[adjacencyList as E3]->D-[adjacencyList as E4]->E-[adjacencyList as E5]->F
                WHERE
	                A.gyear = 1990
                and B.gyear > 1985
                and C.gyear > 1986
                and D.gyear > 1985
                and E.gyear > 1985
                and F.gyear = 1990
                and A.patentid != B.patentid and B.patentid != C.patentid and C.patentid != D.patentid and D.patentid != E.patentid and E.patentid != F.patentid
";
        }
    }
}
