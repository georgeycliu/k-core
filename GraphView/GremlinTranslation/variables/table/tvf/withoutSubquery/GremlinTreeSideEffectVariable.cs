﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinTreeSideEffectVariable : GremlinScalarTableVariable
    {
        public string SideEffectKey { get; set; }
        public GremlinPathVariable PathVariable { get; set; }

        public GremlinTreeSideEffectVariable(string sideEffectKey, GremlinPathVariable pathVariable)
        {
            SideEffectKey = sideEffectKey;
            PathVariable = pathVariable;
            Labels.Add(sideEffectKey);
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() {this};
            variableList.AddRange(PathVariable.FetchAllVars());
            return variableList;
        }

        internal override void Populate(string property)
        {
            PathVariable.Populate(property);
            base.Populate(property);
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            parameters.Add(SqlUtil.GetValueExpr(SideEffectKey));
            parameters.Add(PathVariable.GetDefaultProjection().ToScalarExpression());
            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.Tree, parameters, GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }
}
