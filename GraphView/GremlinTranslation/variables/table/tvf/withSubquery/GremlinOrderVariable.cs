using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinOrderVariable: GremlinTableVariable
    {
        public List<Tuple<GremlinToSqlContext, IComparer>> ByModulatingList;
        public GremlinVariable InputVariable { get; set; }

        public GremlinOrderVariable(GremlinVariable inputVariable, List<Tuple<GremlinToSqlContext, IComparer>> byModulatingList)
            :base(GremlinVariableType.Table)
        {
            this.ByModulatingList = byModulatingList;
            this.InputVariable = inputVariable;
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable> {this, this.InputVariable};
            foreach (var by in this.ByModulatingList)
            {
                variableList.AddRange(by.Item1.FetchAllVars());
            }
            return variableList;
        }

        internal override List<GremlinVariable> FetchAllTableVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            foreach (var by in this.ByModulatingList)
            {
                variableList.AddRange(by.Item1.FetchAllTableVars());
            }
            return variableList;
        }
    }

    internal class GremlinOrderGlobalVariable : GremlinOrderVariable
    {
        public GremlinOrderGlobalVariable(GremlinVariable inputVariable, List<Tuple<GremlinToSqlContext, IComparer>> byModulatingList)
            : base(inputVariable, byModulatingList)
        {
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();

            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.OrderGlobal, parameters, GetVariableName()) as WOrderGlobalTableReference;

            foreach (var pair in this.ByModulatingList)
            {
                WScalarExpression scalarExpr = SqlUtil.GetScalarSubquery(pair.Item1.ToSelectQueryBlock());
                tableRef.OrderParameters.Add(new Tuple<WScalarExpression, IComparer>(scalarExpr, pair.Item2));
                tableRef.Parameters.Add(scalarExpr);
            }

            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }

    internal class GremlinOrderLocalVariable : GremlinOrderVariable
    {
        public GremlinOrderLocalVariable(GremlinVariable inputVariable, List<Tuple<GremlinToSqlContext, IComparer>> byModulatingList)
            : base(inputVariable, byModulatingList)
        {
        }

        internal override void Populate(string property)
        {
            this.InputVariable.Populate(property);
            base.Populate(property);
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();

            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.OrderLocal, parameters, GetVariableName()) as WOrderLocalTableReference;

            tableRef.Parameters.Add(this.InputVariable.GetDefaultProjection().ToScalarExpression());
            foreach (var pair in this.ByModulatingList)
            {
                WScalarExpression scalarExpr = SqlUtil.GetScalarSubquery(pair.Item1.ToSelectQueryBlock());
                tableRef.OrderParameters.Add(new Tuple<WScalarExpression, IComparer>(scalarExpr, pair.Item2));
                tableRef.Parameters.Add(scalarExpr);
            }

            foreach (var property in this.ProjectedProperties)
            {
                tableRef.Parameters.Add(SqlUtil.GetValueExpr(property));
            }

            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }
    }
}
