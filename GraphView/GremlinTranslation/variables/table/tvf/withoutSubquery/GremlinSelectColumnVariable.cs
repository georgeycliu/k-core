﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinSelectColumnVariable : GremlinTableVariable
    {
        public GremlinVariable InputVariable { get; set; }
        public GremlinKeyword.Column Column { get; set; }

        public GremlinSelectColumnVariable(GremlinVariable inputVariable, GremlinKeyword.Column column) : base(GremlinVariableType.Table)
        {
            InputVariable = inputVariable;
            Column = column;
        }

        internal override void Populate(string property)
        {
            base.Populate(property);
            InputVariable.Populate(property);
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            variableList.AddRange(InputVariable.FetchAllVars());
            return variableList;
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            parameters.Add(InputVariable.GetDefaultProjection().ToScalarExpression());
            parameters.Add(SqlUtil.GetValueExpr(Column == GremlinKeyword.Column.Keys ? "Keys" : "Values"));
            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.SelectColumn, parameters, GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }

    internal class GremlinOrderLocalInitVariable : GremlinVariable
    {
        public GremlinOrderLocalInitVariable()
        {
            VariableName = GremlinKeyword.Compose1TableDefaultName;
        }

        internal override GremlinVariableProperty GetDefaultProjection()
        {
            return new GremlinVariableProperty(this, GremlinKeyword.TableDefaultColumnName);
        }
    }
}
