using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinDropVariable : GremlinTableVariable
    {
        public GremlinVariable DroppedVariable { get; set; }

        public GremlinDropVariable(GremlinVariable droppedVariable) : base(GremlinVariableType.Null)
        {
            DroppedVariable = droppedVariable;
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            variableList.AddRange(DroppedVariable.FetchAllVars());
            return variableList;
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            parameters.Add(DroppedVariable.GetDefaultProjection().ToScalarExpression());
            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.Drop, parameters, GetVariableName());
            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }
}
