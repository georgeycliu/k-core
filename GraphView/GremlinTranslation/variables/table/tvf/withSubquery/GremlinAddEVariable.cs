using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GraphView
{
    internal class GremlinAddETableVariable: GremlinEdgeTableVariable
    {
        public GremlinVariable InputVariable { get; set; }
        public GremlinToSqlContext FromVertexContext { get; set; }
        public GremlinToSqlContext ToVertexContext { get; set; }
        public List<GremlinProperty> EdgeProperties { get; set; }
        public string EdgeLabel { get; set; }

        private int OtherVIndex;

        public GremlinAddETableVariable(GremlinVariable inputVariable, string edgeLabel, List<GremlinProperty> edgeProperties, GremlinToSqlContext fromContext, GremlinToSqlContext toContext)
        {
            EdgeProperties = edgeProperties;
            EdgeLabel = edgeLabel;
            InputVariable = inputVariable;
            EdgeType = WEdgeType.OutEdge;
            OtherVIndex = 1;
            FromVertexContext = fromContext;
            ToVertexContext = toContext;
        }

        internal override void Populate(string property)
        {
            base.Populate(property);
        }

        internal override List<GremlinVariable> FetchAllVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() {this};
            variableList.Add(InputVariable);
            if (FromVertexContext != null)
                variableList.AddRange(FromVertexContext.FetchAllVars());
            if (ToVertexContext != null)
                variableList.AddRange(ToVertexContext.FetchAllVars());
            return variableList;
        }

        internal override List<GremlinVariable> FetchAllTableVars()
        {
            List<GremlinVariable> variableList = new List<GremlinVariable>() { this };
            if (FromVertexContext != null)
                variableList.AddRange(FromVertexContext.FetchAllTableVars());
            if (ToVertexContext != null)
                variableList.AddRange(ToVertexContext.FetchAllTableVars());
            return variableList;
        }

        public override WTableReference ToTableReference()
        {
            List<WScalarExpression> parameters = new List<WScalarExpression>();
            parameters.Add(SqlUtil.GetScalarSubquery(GetSelectQueryBlock(FromVertexContext)));
            parameters.Add(SqlUtil.GetScalarSubquery(GetSelectQueryBlock(ToVertexContext)));

            if (ToVertexContext == null && FromVertexContext != null) OtherVIndex = 0;
            if (ToVertexContext != null && FromVertexContext == null) OtherVIndex = 1;
            if (ToVertexContext != null && FromVertexContext != null) OtherVIndex = 1;

            parameters.Add(SqlUtil.GetValueExpr(OtherVIndex));

            parameters.Add(this.EdgeLabel != null ? SqlUtil.GetValueExpr(this.EdgeLabel) : SqlUtil.GetValueExpr(null));

            foreach (var property in EdgeProperties)
            {
                parameters.Add(property.ToPropertyExpr());
                //parameters.Add(SqlUtil.GetValueExpr(property.Value));
            }
            foreach (string property in this.ProjectedProperties) {
                parameters.Add(SqlUtil.GetValueExpr(property));    
            }

            var tableRef = SqlUtil.GetFunctionTableReference(GremlinKeyword.func.AddE, parameters, GetVariableName());

            return SqlUtil.GetCrossApplyTableReference(tableRef);
        }

        private WSelectQueryBlock GetSelectQueryBlock(GremlinToSqlContext context)
        {
            if (context == null)
            {
                var queryBlock = new WSelectQueryBlock();
                queryBlock.SelectElements.Add(SqlUtil.GetSelectScalarExpr(InputVariable.GetDefaultProjection().ToScalarExpression()));
                return queryBlock;
            }
            else
            {
                return context.ToSelectQueryBlock();
            } 
        }
    }
}
